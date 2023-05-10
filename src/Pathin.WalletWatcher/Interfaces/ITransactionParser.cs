// <copyright file="ITransactionParser.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using Nethereum.RPC.Eth.DTOs;
using Pathin.WalletWatcher.Config;
using Pathin.WalletWatcher.Services.TransactionParsers;

namespace Pathin.WalletWatcher.Interfaces;

/// <summary>
/// Defines an interface for parsing transactions.
/// </summary>
public interface ITransactionParser
{
    /// <summary>
    /// Parses transaction asynchronously.
    /// </summary>
    /// <param name="transaction">The transaction to parse.</param>
    /// <param name="receipt">The transaction receipt.</param>
    /// <param name="walletConfig">The wallet configuration.</param>
    /// <param name="evmEndpointConfig">The EVM endpoint configuration.</param>
    /// <returns>A <see cref="Task{TransactionDisplayInfo}"/> representing the parsed transaction.</returns>
     Task<TransactionDisplayInfo?> ParseTransactionAsync(Transaction transaction, TransactionReceipt receipt, WalletConfig walletConfig, EvmEndpointConfig evmEndpointConfig);
}