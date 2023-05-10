// <copyright file="TransactionDisplayInfo.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

namespace Pathin.WalletWatcher.Services.TransactionParsers;

/// <summary>
/// Represents display information for a parsed transaction.
/// </summary>
/// <param name="Title">The title of the transaction display.</param>
/// <param name="Transactions">A list of parsed transactions.</param>
/// <param name="Url">The URL for additional transaction information.</param>
/// <param name="GasUsed">The amount of gas used in the transaction.</param>
/// <param name="TransactionHash">The hash of the transaction.</param>
public record TransactionDisplayInfo(string Title, List<TransactionInfo> Transactions, string? Url, decimal GasUsed, string TransactionHash);