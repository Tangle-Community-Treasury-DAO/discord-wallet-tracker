// <copyright file="TransactionType.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

namespace Pathin.WalletWatcher.Enums;

/// <summary>
/// Represents the type of a transaction.
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Indicates that the transaction was a send transaction.
    /// </summary>
    Send,

    /// <summary>
    /// Indicates that the transaction was a receive transaction.
    /// </summary>
    Receive,
}