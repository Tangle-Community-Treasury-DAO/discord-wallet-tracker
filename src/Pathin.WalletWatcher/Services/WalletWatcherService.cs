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
using Pathin.WalletWatcher.Interfaces;
using Pathin.WalletWatcher.Services.TransactionParsers;

namespace Pathin.WalletWatcher.Services;

/// <summary>
/// The WalletWatcherService class monitors wallets on Ethereum Virtual Machine (EVM) networks and sends notifications via Discord Webhooks.
/// </summary>
[SingletonService]
public partial class WalletWatcherService : IAppService
{
    private readonly ILogger<App> _logger;
    private readonly IOptions<AppSettings> _appSettings;
    private readonly ITransactionParser _defaultTransactionParser;
    private readonly ITransactionParser _erc20TransactionParser;
    private readonly ITransactionParser _gnosisSafeTransactionParser;

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
        _defaultTransactionParser = new DefaultTransactionParser();
        _erc20TransactionParser = new Erc20TransactionParser();
        _gnosisSafeTransactionParser = new GnosisSafeTransactionParser();
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
                var transferEvent = web3.Eth.GetEvent<TransferEventDTO>();
                while (true)
                {
                    var latestBlockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                    if (currentBlockNumber.Value != latestBlockNumber.Value)
                    {
                        for (BigInteger i = currentBlockNumber.Value + 1; i <= latestBlockNumber.Value; i++)
                        {
                            var block = await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new HexBigInteger(i));

                            var receiptTasks = block.Transactions.Select(transaction => web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transaction.TransactionHash));
                            var receipts = await Task.WhenAll(receiptTasks);

                            for (var j = 0; j < block.Transactions.Length; j++)
                            {

                                var transaction = block.Transactions[j];
                                var receipt = receipts[j];

                                if(receipt == null || receipt.Status.Value == 0)
                                {
                                    continue;
                                }

    //                             var relevantLogs = receipt.Logs.Where(log => log["topics"].Any() && log["topics"][0].ToString().Equals("0xddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef", StringComparison.InvariantCultureIgnoreCase) &&
    // log["topics"].Count() > 2 && Web3.ToChecksumAddress(log["topics"][2].ToString()) == walletConfig.Address);

    //                             // Convert JToken logs to FilterLog objects
    //                             var filterLogs = relevantLogs.Select(log => log.ToObject<FilterLog>()).ToArray();

    //                             var decodedLogs = transferEvent.DecodeAllEventsForEvent(filterLogs);
    //                             bool isTransferToAddress = decodedLogs.Any();

                                // Decode and filter the logs
                                var decodedLogs = transferEvent.DecodeAllEventsForEvent(receipt.Logs);
                                var isTransferToAddress = decodedLogs.Any(log => log.Event.To.Equals(walletConfig.Address, StringComparison.OrdinalIgnoreCase));

                                if (isTransferToAddress || (transaction.From != null && transaction.From.Equals(walletConfig.Address, StringComparison.OrdinalIgnoreCase)) ||
                                    (transaction.To != null && transaction.To.Equals(walletConfig.Address, StringComparison.OrdinalIgnoreCase)))
                                {
                                    // Get the correct transaction parser based on the transaction type
                                    var transactionParser = GetTransactionParser(transaction);

                                    var transactionDisplayInfo = await transactionParser.ParseTransactionAsync(transaction, receipt, walletConfig, evmEndpointConfig);
                                    if (transactionDisplayInfo != null)
                                    {
                                        await SendDiscordWebhook(transactionDisplayInfo, walletConfig, evmEndpointConfig);
                                    }
                                }
                            }
                        }

                        currentBlockNumber = latestBlockNumber;
                    }

                    await Task.Delay(1000, cancellationToken);
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
                await Task.Delay(1000, cancellationToken); // Delay for 30 seconds before retrying
            }
        }
    }

    private async Task SendDiscordWebhook(TransactionDisplayInfo transactionDisplayInfo, WalletConfig walletConfig, EvmEndpointConfig evmEndpointConfig)
    {
        try
        {
            var web3 = new Web3(evmEndpointConfig.RpcUrl);
            var nativeToken = evmEndpointConfig.NativeToken;

            string description = string.Empty;
            foreach (var transaction in transactionDisplayInfo.Transactions)
            {
                string addressDescription = transaction.Type == TransactionType.Send ? "To" : "From";
                description = $"**Type:** {transaction.Type}\n**{addressDescription}:** {transaction.PartyAddress}\n**Value:** {transaction.Value} {transaction.Symbol}\n**New total:** {transaction.NewTotal} {transaction.Symbol}";
            }

            var embed = new EmbedBuilder
            {
                Title = transactionDisplayInfo.Title,
                Description = description,
                Color = 0x2ecc71,
                Url = transactionDisplayInfo.Url,
                Timestamp = DateTime.UtcNow,
                Footer = new EmbedFooterBuilder() { Text = "By Pathin with ❤️" },
            };

            embed.AddField("Gas Used", $"{transactionDisplayInfo.GasUsed} {nativeToken}");

            var webhookClient = new DiscordWebhookClient(walletConfig.WebhookUrl);
            await webhookClient.SendMessageAsync(null, embeds: new[] { embed.Build() });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error sending Discord webhook for transaction {TransactionHash}", transactionDisplayInfo.TransactionHash);
        }
    }

    private ITransactionParser GetTransactionParser(Transaction transaction)
    {
        // Detect transaction type here, e.g., based on the input data
        if (transaction.Input.Length > 2 && transaction.Input.StartsWith("0xa9059cbb")) // ERC20 transfer
        {
            return _erc20TransactionParser;
        }
        else if(GnosisSafeTransactionParser.IsGnosisSafeTransaction(transaction))
        {
            return _gnosisSafeTransactionParser;
        }

        return _defaultTransactionParser;
    }
}