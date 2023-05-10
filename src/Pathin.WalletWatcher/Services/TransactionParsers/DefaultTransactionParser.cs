// <copyright file="DefaultTransactionParser.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Pathin.WalletWatcher.Config;
using Pathin.WalletWatcher.Enums;
using Pathin.WalletWatcher.Interfaces;

namespace Pathin.WalletWatcher.Services.TransactionParsers;

/// <summary>
/// Represents the default implementation of the <see cref="ITransactionParser"/> interface.
/// </summary>
public class DefaultTransactionParser : ITransactionParser
{
    /// <inheritdoc/>
    public async Task<TransactionDisplayInfo?> ParseTransactionAsync(Transaction transaction, TransactionReceipt receipt, WalletConfig walletConfig, EvmEndpointConfig evmEndpointConfig)
    {
        var web3 = new Web3(evmEndpointConfig.RpcUrl);
        var transactionDisplayInfo = new TransactionDisplayInfo(
            $"New {walletConfig.Name} Transaction",
            new List<TransactionInfo>(),
            evmEndpointConfig.ExplorerUrl != null ? string.Format(evmEndpointConfig.ExplorerUrl, transaction.TransactionHash) : null,
            Web3.Convert.FromWei(receipt.GasUsed.Value * transaction.GasPrice.Value),
            transaction.TransactionHash);

        var transactionType = transaction.From.Equals(walletConfig.Address, StringComparison.OrdinalIgnoreCase) ? TransactionType.Send : TransactionType.Receive;
        string partyAddress = transactionType == TransactionType.Send ? transaction.To : transaction.From;

        decimal value = Web3.Convert.FromWei(transaction.Value);
        decimal newBalance = Web3.Convert.FromWei(await web3.Eth.GetBalance.SendRequestAsync(walletConfig.Address));

        transactionDisplayInfo.Transactions.Add(new TransactionInfo(transactionType, partyAddress, value, evmEndpointConfig.NativeToken, newBalance));

        return transactionDisplayInfo;
    }
}