// <copyright file="TransactionInfo.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using Pathin.WalletWatcher.Enums;

namespace Pathin.WalletWatcher.Services.TransactionParsers;

/// <summary>
/// Represents information about a parsed transaction.
/// </summary>
/// <param name="Type">The type of the transaction (send or receive).</param>
/// <param name="PartyAddress">The address of the party involved in the transaction.</param>
/// <param name="Value">The value of the transaction in tokens.</param>
/// <param name="Symbol">The symbol of the token involved in the transaction.</param>
/// <param name="NewTotal">The new total token balance after the transaction.</param>
/// <param name="AdditionalInfo">Additional information about the transaction, if available.</param>
public record TransactionInfo(TransactionType Type, string PartyAddress, decimal Value, string Symbol, decimal NewTotal, string? AdditionalInfo = null);