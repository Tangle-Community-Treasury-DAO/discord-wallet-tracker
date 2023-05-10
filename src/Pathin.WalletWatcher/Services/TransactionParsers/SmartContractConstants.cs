// <copyright file="SmartContractConstants.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

namespace Pathin.WalletWatcher.Services.TransactionParsers;

/// <summary>
/// Provides a set of smart contract constants used in transaction parsing.
/// </summary>
public static class SmartContractConstants
{
/// <summary>
    /// The MultiSend contract function signature.
    /// </summary>
    public const string MultisendContractFunctionSignature = "0x8d80ff0a";

    /// <summary>
    /// The Gnosis Safe exec function signature.
    /// </summary>
    public const string GnosisExecFunctionSignature = "0x6a761202";

    /// <summary>
    /// The ERC20 transfer function signature.
    /// </summary>
    public const string ERC20TransferFunctionSignature = "0xa9059cbb";
}