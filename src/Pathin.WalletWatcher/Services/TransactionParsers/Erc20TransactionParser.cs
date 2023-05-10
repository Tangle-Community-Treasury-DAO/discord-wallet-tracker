// <copyright file="Erc20TransactionParser.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using System.Numerics;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Pathin.WalletWatcher.Config;
using Pathin.WalletWatcher.Enums;
using Pathin.WalletWatcher.Interfaces;

namespace Pathin.WalletWatcher.Services.TransactionParsers;

/// <summary>
/// Represents an implementation of the <see cref="ITransactionParser"/> interface for parsing ERC20 token transactions.
/// </summary>
public class Erc20TransactionParser : ITransactionParser
{
    /// <summary>
    /// Retrieves the number of decimals for the specified token contract.
    /// </summary>
    /// <param name="web3">The Web3 instance.</param>
    /// <param name="contractAddress">The token contract address.</param>
    /// <returns>The number of decimals for the token contract.</returns>
    public static async Task<int> GetTokenDecimals(Web3 web3, string contractAddress)
    {
        var decimalsFunction = new DecimalsFunction();
        var decimalsHandler = web3.Eth.GetContractQueryHandler<DecimalsFunction>();
        return await decimalsHandler.QueryAsync<int>(contractAddress, decimalsFunction);
    }

    /// <summary>
    /// Retrieves the symbol for the specified token contract.
    /// </summary>
    /// <param name="web3">The Web3 instance.</param>
    /// <param name="contractAddress">The token contract address.</param>
    /// <returns>The symbol for the token contract.</returns>
    public static async Task<string> GetTokenSymbol(Web3 web3, string contractAddress)
    {
        var symbolFunction = new SymbolFunction();
        var symbolHandler = web3.Eth.GetContractQueryHandler<SymbolFunction>();
        return await symbolHandler.QueryAsync<string>(contractAddress, symbolFunction);
    }

    /// <summary>
    /// Retrieves the token balance for the specified owner address.
    /// </summary>
    /// <param name="web3">The Web3 instance.</param>
    /// <param name="contractAddress">The token contract address.</param>
    /// <param name="ownerAddress">The owner address to query the balance for.</param>
    /// <returns>The token balance for the specified owner address.</returns>
    public static async Task<BigInteger> GetTokenBalanceAsync(Web3 web3, string contractAddress, string ownerAddress)
    {
        var balanceOfFunction = new BalanceOfFunction { Owner = ownerAddress };
        var balanceHandler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();
        return await balanceHandler.QueryAsync<BigInteger>(contractAddress, balanceOfFunction);
    }

    /// <inheritdoc/>
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
}