// <copyright file="ITransactionParser.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using Nethereum.RPC.Eth.DTOs;
using Pathin.WalletWatcher.Config;
using Pathin.WalletWatcher.Services.TransactionParsers;

namespace Pathin.WalletWatcher.Interfaces;

public interface ITransactionParser
{
     Task<TransactionDisplayInfo?> ParseTransactionAsync(Transaction transaction, TransactionReceipt receipt, WalletConfig walletConfig, EvmEndpointConfig evmEndpointConfig);
}