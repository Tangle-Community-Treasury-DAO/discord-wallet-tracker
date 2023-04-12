// <copyright file="WalletWatcherService.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using System.Numerics;
using Discord;
using Discord.Webhook;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Pathin.WalletWatcher.Attributes;
using Pathin.WalletWatcher.Config;

namespace Pathin.WalletWatcher.Services;

/// <summary>
/// The WalletWatcherService class monitors wallets on Ethereum Virtual Machine (EVM) networks and sends notifications via Discord Webhooks.
/// </summary>
[SingletonService]
public partial class WalletWatcherService : IAppService
{
    private readonly ILogger<App> _logger;
    private readonly IOptions<AppSettings> _appSettings;

    private Task? _walletWatcherTask;
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="WalletWatcherService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="appSettings">The application settings instance.</param>
    public WalletWatcherService(ILogger<App> logger, IOptions<AppSettings> appSettings)
    {
        _logger = logger;
        _appSettings = appSettings;
    }

    /// <inheritdoc/>
    public async Task StartAsync()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _walletWatcherTask = RunAsync(_cancellationTokenSource.Token);
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopAsync()
    {
        if (_walletWatcherTask != null)
        {
            _cancellationTokenSource?.Cancel();
            await _walletWatcherTask;
            _walletWatcherTask = null;
        }
    }

    /// <inheritdoc/>
    public string GetStatus()
    {
        return _walletWatcherTask == null ? "Stopped" : "Running";
    }

    private static async Task<int> GetTokenDecimals(Web3 web3, string contractAddress)
    {
        var decimalsFunction = new DecimalsFunction();
        var decimalsHandler = web3.Eth.GetContractQueryHandler<DecimalsFunction>();
        return await decimalsHandler.QueryAsync<int>(contractAddress, decimalsFunction);
    }

    private static async Task<string> GetTokenSymbol(Web3 web3, string contractAddress)
    {
        var symbolFunction = new SymbolFunction();
        var symbolHandler = web3.Eth.GetContractQueryHandler<SymbolFunction>();
        return await symbolHandler.QueryAsync<string>(contractAddress, symbolFunction);
    }

    private static async Task<BigInteger> GetTokenBalanceAsync(Web3 web3, string contractAddress, string ownerAddress)
    {
        var balanceOfFunction = new BalanceOfFunction { Owner = ownerAddress };
        var balanceHandler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();
        return await balanceHandler.QueryAsync<BigInteger>(contractAddress, balanceOfFunction);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var walletConfigs = _appSettings.Value.Wallets.Where(w => w.Active);

        List<Task> processWalletTasks = new();
        foreach (var walletConfig in walletConfigs)
        {
            var evmEndpoints = _appSettings.Value.GetEvmEndpointConfigsForWallet(walletConfig);
            var ethereumAddress = walletConfig.Address;
            foreach (var evmEndpoint in evmEndpoints)
            {
                processWalletTasks.Add(ProcessWallet(walletConfig, evmEndpoint, cancellationToken));
            }
        }

        await Task.WhenAll(processWalletTasks);
    }

    private async Task ProcessWallet(WalletConfig walletConfig, EvmEndpointConfig evmEndpointConfig, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var web3 = new Web3(evmEndpointConfig.RpcUrl);
                var currentBlockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                while (true)
                {
                    var latestBlockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                    if (currentBlockNumber.Value != latestBlockNumber.Value)
                    {
                        for (BigInteger i = currentBlockNumber.Value + 1; i <= latestBlockNumber.Value; i++)
                        {
                            var block = await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new HexBigInteger(i));
                            foreach (var transaction in block.Transactions)
                            {
                                if ((transaction.From != null && transaction.From.Equals(walletConfig.Address, StringComparison.OrdinalIgnoreCase)) ||
                                    (transaction.To != null && transaction.To.Equals(walletConfig.Address, StringComparison.OrdinalIgnoreCase)))
                                {
                                    var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transaction.TransactionHash);
                                    await SendDiscordWebhook(transaction, receipt, walletConfig, evmEndpointConfig);
                                }
                            }
                        }

                        currentBlockNumber = latestBlockNumber;
                    }

                    await Task.Delay(15000, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Wallet {WalletAddress} on {EvmEndpoint} cancelled", walletConfig.Address, evmEndpointConfig.RpcUrl);
                break; // If the operation is cancelled, break out of the while loop
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error processing wallet {WalletAddress} on {EvmEndpoint}. Retrying after delay.", walletConfig.Address, evmEndpointConfig.RpcUrl);
                await Task.Delay(15000, cancellationToken); // Delay for 30 seconds before retrying
            }
        }
    }

    private async Task SendDiscordWebhook(Transaction transaction, TransactionReceipt receipt, WalletConfig walletConfig, EvmEndpointConfig evmEndpointConfig)
    {
        try
        {
            var web3 = new Web3(evmEndpointConfig.RpcUrl);
            var nativeToken = evmEndpointConfig.NativeToken;

            var walletAddress = walletConfig.Address;
            var webhookUrl = walletConfig.WebhookUrl;

            string transactionType = transaction.From.Equals(walletAddress, StringComparison.OrdinalIgnoreCase) ? "Send" : "Receive";
            string tokenAddress = transaction.From.Equals(walletAddress, StringComparison.OrdinalIgnoreCase) ? transaction.From : transaction.To;
            string addressDescription = transactionType == "Send" ? "To" : "From";

            string description;
            if (receipt.Logs.Count > 0)
            {
                var eventLog = web3.Eth.GetEvent<TransferEventDTO>().DecodeAllEventsForEvent(receipt.Logs).FirstOrDefault();
                if (eventLog != null)
                {
                    var contractAddress = eventLog.Log.Address;
                    var decimals = await GetTokenDecimals(web3, contractAddress);
                    var symbol = await GetTokenSymbol(web3, contractAddress);
                    var newBalance = await GetTokenBalanceAsync(web3, contractAddress, walletAddress);
                    var partyAddress = transactionType == "Send" ? eventLog.Event.To : eventLog.Event.From;

                    decimal value = (decimal)eventLog.Event.Value / (decimal)Math.Pow(10, decimals);
                    decimal total = (decimal)newBalance / (decimal)Math.Pow(10, decimals);

                    string valueFormatted = value.ToString("N4");
                    string totalFormatted = total.ToString("N4");

                    description = $"**Type:** {transactionType}\n**{addressDescription}:** {partyAddress}\n**Value:** {valueFormatted} {symbol}\n**New total:** {totalFormatted} {symbol}";
                }
                else
                {
                    description = $"Unsupported token transfer";
                }
            }
            else
            {
                var newBalance = await web3.Eth.GetBalance.SendRequestAsync(tokenAddress);
                var partyAddress = transactionType == "Send" ? transaction.To : transaction.From;

                decimal value = Web3.Convert.FromWei(transaction.Value);
                decimal total = Web3.Convert.FromWei(newBalance.Value);

                string valueFormatted = value.ToString("N4");
                string totalFormatted = total.ToString("N4");

                description = $"**Type:** {transactionType}\n**{addressDescription}:** {partyAddress}\n**Value:** {valueFormatted} {nativeToken}\n**New total:** {totalFormatted} {nativeToken}";
            }

            var gasPrice = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(transaction.TransactionHash);
            decimal gasUsed = Web3.Convert.FromWei(receipt.GasUsed.Value * gasPrice.GasPrice.Value);

            var embed = new EmbedBuilder
            {
                Title = $"New {walletConfig.Name} Transaction",
                Description = description,
                Color = 0x2ecc71,
                Url = evmEndpointConfig.ExplorerUrl != null ? string.Format(evmEndpointConfig.ExplorerUrl, transaction.TransactionHash) : null,
                Timestamp = DateTime.UtcNow,
                Footer = new EmbedFooterBuilder() { Text = "By Pathin with ❤️" },
            };

            embed.AddField("Gas Used", $"{gasUsed} {nativeToken}");

            var webhookClient = new DiscordWebhookClient(webhookUrl);
            await webhookClient.SendMessageAsync(null, embeds: new[] { embed.Build() });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error sending Discord webhook for transaction {TransactionHash}", transaction.TransactionHash);
        }
    }
}