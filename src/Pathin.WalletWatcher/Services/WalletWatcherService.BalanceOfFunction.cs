// <copyright file="WalletWatcherService.BalanceOfFunction.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace Pathin.WalletWatcher.Services;

/// <summary>
/// The WalletWatcherService class monitors wallets on Ethereum Virtual Machine (EVM) networks and sends notifications via Discord Webhooks.
/// </summary>
public partial class WalletWatcherService
{
    /// <summary>
    /// The BalanceOfFunction class represents the balanceOf function of the ERC20 token standard.
    /// </summary>
    [Function("balanceOf", "uint256")]
    public class BalanceOfFunction : FunctionMessage
    {
        /// <summary>
        /// Gets the owner address.
        /// </summary>
        [Parameter("address", "_owner", 1)]
        public string Owner { get; init; } = default!;
    }
}