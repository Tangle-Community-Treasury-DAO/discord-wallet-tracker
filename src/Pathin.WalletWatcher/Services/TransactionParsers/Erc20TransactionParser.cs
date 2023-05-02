using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Pathin.WalletWatcher.Config;
using Pathin.WalletWatcher.Interfaces;

namespace Pathin.WalletWatcher.Services.TransactionParsers;

public class Erc20TransactionParser : ITransactionParser
{
    public async Task<TransactionDisplayInfo?> ParseTransactionAsync(Transaction transaction, TransactionReceipt receipt, WalletConfig walletConfig, EvmEndpointConfig evmEndpointConfig)
    {
        var web3 = new Web3(evmEndpointConfig.RpcUrl);
        var eventLog = web3.Eth.GetEvent<TransferEventDTO>().DecodeAllEventsForEvent(receipt.Logs).FirstOrDefault();

        if (eventLog == null)
        {
            return null; // Not an ERC20 transaction
        }

        var transactionDisplayInfo = new TransactionDisplayInfo(
            $"New {walletConfig.Name} Transaction",
            new List<TransactionInfo>(),
            evmEndpointConfig.ExplorerUrl != null ? string.Format(evmEndpointConfig.ExplorerUrl, transaction.TransactionHash) : null,
            Web3.Convert.FromWei(receipt.GasUsed.Value * transaction.GasPrice.Value),
            transaction.TransactionHash);

        TransactionType transactionType = eventLog.Event.From.Equals(walletConfig.Address, StringComparison.OrdinalIgnoreCase) ? TransactionType.Send : TransactionType.Receive;
        string partyAddress = transactionType == TransactionType.Send ? eventLog.Event.To : eventLog.Event.From;

        string contractAddress = eventLog.Log.Address;
        int decimals = await GetTokenDecimals(web3, contractAddress);
        string symbol = await GetTokenSymbol(web3, contractAddress);
        BigInteger newBalance = await GetTokenBalanceAsync(web3, contractAddress, walletConfig.Address);

        decimal value = (decimal)eventLog.Event.Value / (decimal)Math.Pow(10, decimals);
        decimal total = (decimal)newBalance / (decimal)Math.Pow(10, decimals);
        transactionDisplayInfo.Transactions.Add(new TransactionInfo(transactionType, partyAddress, value, symbol, total));

        return transactionDisplayInfo;
    }

    public static async Task<int> GetTokenDecimals(Web3 web3, string contractAddress)
    {
        var decimalsFunction = new DecimalsFunction();
        var decimalsHandler = web3.Eth.GetContractQueryHandler<DecimalsFunction>();
        return await decimalsHandler.QueryAsync<int>(contractAddress, decimalsFunction);
    }

    public static async Task<string> GetTokenSymbol(Web3 web3, string contractAddress)
    {
        var symbolFunction = new SymbolFunction();
        var symbolHandler = web3.Eth.GetContractQueryHandler<SymbolFunction>();
        return await symbolHandler.QueryAsync<string>(contractAddress, symbolFunction);
    }

    public static async Task<BigInteger> GetTokenBalanceAsync(Web3 web3, string contractAddress, string ownerAddress)
    {
        var balanceOfFunction = new BalanceOfFunction { Owner = ownerAddress };
        var balanceHandler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();
        return await balanceHandler.QueryAsync<BigInteger>(contractAddress, balanceOfFunction);
    }

}
