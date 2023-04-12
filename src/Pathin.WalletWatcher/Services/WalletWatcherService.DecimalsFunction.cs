// <copyright file="WalletWatcherService.DecimalsFunction.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
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
    /// The DecimalsFunction class represents the decimals function of the ERC20 token standard.
    /// </summary>
    [Function("decimals", "uint8")]
    public class DecimalsFunction : FunctionMessage
    {
    }
}