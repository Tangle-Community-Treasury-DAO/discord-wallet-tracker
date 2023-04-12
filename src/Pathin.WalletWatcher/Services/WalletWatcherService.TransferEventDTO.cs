// <copyright file="WalletWatcherService.TransferEventDTO.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace Pathin.WalletWatcher.Services;

/// <summary>
/// The WalletWatcherService class monitors wallets on Ethereum Virtual Machine (EVM) networks and sends notifications via Discord Webhooks.
/// </summary>
public partial class WalletWatcherService
{
    /// <summary>
    /// The TransferEventDTO class represents the Transfer event of the ERC20 token standard.
    /// </summary>
    [Event("Transfer")]
    public class TransferEventDTO : IEventDTO
    {
        /// <summary>
        /// Gets the from address.
        /// </summary>
        [Parameter("address", "_from", 1, true)]
        public string From { get; init; } = default!;

        /// <summary>
        /// Gets the to address.
        /// </summary>
        [Parameter("address", "_to", 2, true)]
        public string To { get; init; } = default!;

        /// <summary>
        /// Gets the value.
        /// </summary>
        [Parameter("uint256", "_value", 3, false)]
        public BigInteger Value { get; init; } = default!;
    }
}