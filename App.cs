using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Discord;
using Discord.Webhook;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Pathin.WalletWatcher.Config;

namespace Pathin.WalletWatcher;

public class App
{
    private readonly ILogger<App> _logger;
    private readonly IOptions<AppSettings> _appSettings;

    public App(ILogger<App> logger, IOptions<AppSettings> appSettings)
    {
        _logger = logger;
        _appSettings = appSettings;
    }

    public async Task RunAsync()
    {
        var walletConfigs = _appSettings.Value.Wallets.Where(w => w.Active);

        List<Task> processWalletTasks = new();
        foreach (var walletConfig in walletConfigs)
        {
            var evmEndpoints = _appSettings.Value.GetEvmEndpointConfigsForWallet(walletConfig);
            var ethereumAddress = walletConfig.Address;
            foreach (var evmEndpoint in evmEndpoints)
            {
                processWalletTasks.Add(ProcessWallet(walletConfig, evmEndpoint));
            }
        }

        await Task.WhenAll(processWalletTasks);
    }

    private async static Task ProcessWallet(WalletConfig walletConfig, EvmEndpointConfig evmEndpointConfig)
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
                            if (transaction.From != null && transaction.From.Equals(walletConfig.Address, StringComparison.OrdinalIgnoreCase) ||
                                transaction.To != null && transaction.To.Equals(walletConfig.Address, StringComparison.OrdinalIgnoreCase))
                            {
                                var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transaction.TransactionHash);
                                await SendDiscordWebhook(transaction, receipt, walletConfig, evmEndpointConfig.RpcUrl);
                            }
                        }
                    }

                    currentBlockNumber = latestBlockNumber;
                }
                await Task.Delay(15000);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    private static async Task SendDiscordWebhook(Transaction transaction, TransactionReceipt receipt, WalletConfig walletConfig, string evmRpcUrl)
    {
        try
        {
            var web3 = new Web3(evmRpcUrl);

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

                description = $"**Type:** {transactionType}\n**{addressDescription}:** {partyAddress}\n**Value:** {valueFormatted} BNB\n**New total:** {totalFormatted} BNB";
            }

            var gasPrice = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(transaction.TransactionHash);
            decimal gasUsed = Web3.Convert.FromWei(receipt.GasUsed.Value * gasPrice.GasPrice.Value);

            var embed = new EmbedBuilder
            {
                Title = $"New {walletConfig.Name} Transaction",
                Description = description,
                Color = 0x2ecc71,
                Url = $"https://bscscan.com/tx/{transaction.TransactionHash}",
                Timestamp = DateTime.UtcNow,
                Footer = new EmbedFooterBuilder() { Text = "By Pathin with ❤️" },
            };

            embed.AddField("Gas Used", $"{gasUsed} BNB");

            var webhookClient = new DiscordWebhookClient(webhookUrl);
            await webhookClient.SendMessageAsync(null, embeds: new[] { embed.Build() });
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    [Function("decimals", "uint8")]
    public class DecimalsFunction : FunctionMessage { }

    [Function("symbol", "string")]
    public class SymbolFunction : FunctionMessage { }

    [Event("Transfer")]
    public class TransferEventDTO : IEventDTO
    {
        [Parameter("address", "_from", 1, true)]
        public string From { get; set; }

        [Parameter("address", "_to", 2, true)]
        public string To { get; set; }

        [Parameter("uint256", "_value", 3, false)]
        public BigInteger Value { get; set; }
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

    [Function("balanceOf", "uint256")]
    public class BalanceOfFunction : FunctionMessage
    {
        [Parameter("address", "_owner", 1)]
        public string Owner { get; set; }
    }

    private static async Task<BigInteger> GetTokenBalanceAsync(Web3 web3, string contractAddress, string ownerAddress)
    {
        var balanceOfFunction = new BalanceOfFunction { Owner = ownerAddress };
        var balanceHandler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();
        return await balanceHandler.QueryAsync<BigInteger>(contractAddress, balanceOfFunction);
    }
}
