// <copyright file="WalletWatcherService.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using System.Numerics;
using System.Text;
using Discord;
using Discord.Webhook;
using ICCD.UltimatePriceBot.App.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Pathin.WalletWatcher.Attributes;
using Pathin.WalletWatcher.Config;
using Pathin.WalletWatcher.Enums;
using Pathin.WalletWatcher.Extensions;
using Pathin.WalletWatcher.Interfaces;
using Pathin.WalletWatcher.Services.PriceData;
using Pathin.WalletWatcher.Services.PriceData.Source;
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
    private readonly PriceDataService _priceDataService;
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
    /// <param name="priceDataService">The price data service instance.</param>
    public WalletWatcherService(ILogger<App> logger, IOptions<AppSettings> appSettings, PriceDataService priceDataService)
    {
        _logger = logger;
        _appSettings = appSettings;
        _priceDataService = priceDataService;
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
        if (walletConfig == null || evmEndpointConfig == null)
        {
            _logger.LogError("Either walletConfig or evmEndpointConfig is null.");
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var web3 = new Web3(evmEndpointConfig.RpcUrl);
                var batchRpcClient = new BatchRpcClient(evmEndpointConfig.RpcUrl, _logger);
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

                            if (block?.Transactions == null)
                            {
                                continue;
                            }

                            var filteredTransactions = block.Transactions
                            .Select((transaction, index) => new { Transaction = transaction, Index = index })
                            .Where(item => item.Transaction.TransactionHash != null)
                            .ToList();

                            var receipts = await batchRpcClient.GetTransactionReceiptsAsync(filteredTransactions.Select(item => item.Transaction.TransactionHash).ToArray());

                            foreach (var item in filteredTransactions)
                            {
                                var transaction = item.Transaction;
                                var index = item.Index;
                                var receipt = receipts[index];

                                if (receipt == null || receipt.Status.Value == 0)
                                {
                                    continue;
                                }

                                // Decode and filter the logs
                                var decodedLogs = transferEvent.DecodeAllEventsForEvent(receipt.Logs);
                                var isTransferToAddress = decodedLogs.Any(log => log.Event.To.Equals(walletConfig.Address, StringComparison.OrdinalIgnoreCase));
                                var isTransferFromAddress = decodedLogs.Any(log => log.Event.From.Equals(walletConfig.Address, StringComparison.OrdinalIgnoreCase));

                                if (isTransferFromAddress || isTransferToAddress || (transaction.From != null && transaction.From.Equals(walletConfig.Address, StringComparison.OrdinalIgnoreCase)) ||
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

                    await Task.Delay(10000, cancellationToken);
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
                await Task.Delay(10000, cancellationToken); // Delay for 10 seconds before retrying
            }
        }
    }

    private async Task SendDiscordWebhook(TransactionDisplayInfo transactionDisplayInfo, WalletConfig walletConfig, EvmEndpointConfig evmEndpointConfig)
    {
        try
        {
            var web3 = new Web3(evmEndpointConfig.RpcUrl);
            var nativeToken = evmEndpointConfig.NativeToken;

            // Group transactions by type and symbol
            var groupedTransactions = transactionDisplayInfo.Transactions
                .GroupBy(tx => new { tx.Type, tx.Symbol })
                .ToList();

            var description = new StringBuilder();

            // Iterate through grouped transactions
            foreach (var group in groupedTransactions)
            {
                var totalValue = group.Sum(tx => tx.Value);
                decimal tokenPriceUsd = 0;

                if (_priceDataService.TokenExists(group.Key.Symbol))
                {
                    tokenPriceUsd = (await _priceDataService.GetPriceDataAsync(group.Key.Symbol)).CurrentPriceUsd ?? 0;
                }

                var totalValueInUsd = totalValue * tokenPriceUsd;
                var newTotalValueUsd = group.First().NewTotal * tokenPriceUsd;

                description.Append($"**{group.Key.Type}: {totalValue} {group.First().Symbol} ({totalValueInUsd:N2} USD)\nRemaining: {group.First().NewTotal} {group.First().Symbol} ({newTotalValueUsd:N2} USD)**\n");

                // Group by party address
                var partyGroups = group.GroupBy(tx => tx.PartyAddress).ToList();

                foreach (var partyGroup in partyGroups)
                {
                    var partyValue = partyGroup.Sum(tx => tx.Value);
                    decimal partyValueInUsd = partyValue * tokenPriceUsd;

                    string addressDescription = group.Key.Type == TransactionType.Send ? "To" : "From";
                    description.Append($"• {addressDescription}: {partyGroup.Key} | Value: {partyValue} {group.First().Symbol} ({partyValueInUsd:N2} USD)\n");
                }

                description.AppendLine();
            }

            var embed = new EmbedBuilder
            {
                Title = transactionDisplayInfo.Title,
                Description = description.ToString(),
                Color = new Color(0x1abc9c),
                Url = transactionDisplayInfo.Url,
                Timestamp = DateTime.UtcNow,
                Footer = new EmbedFooterBuilder() { Text = "By Pathin with ❤️" },
            };

            decimal totalGasValueInUsd = 0;
            if (_priceDataService.TokenExists(nativeToken))
            {
                var tokenPrice = await _priceDataService.GetPriceDataAsync(nativeToken);
                totalGasValueInUsd = transactionDisplayInfo.GasUsed * (tokenPrice.CurrentPriceUsd ?? 0);
            }

            embed.AddField("⛽ Gas Used", $"{transactionDisplayInfo.GasUsed} {nativeToken} ({totalGasValueInUsd:N2} USD)");

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
        if (transaction.Input.Length > 2 && transaction.Input.StartsWith(SmartContractConstants.ERC20TransferFunctionSignature))
        {
            return _erc20TransactionParser;
        }
        else if (GnosisSafeTransactionParser.IsGnosisSafeTransaction(transaction))
        {
            return _gnosisSafeTransactionParser;
        }

        return _defaultTransactionParser;
    }
}