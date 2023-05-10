// <copyright file="StringExtensions.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

namespace Pathin.WalletWatcher.Extensions;

/// <summary>
/// Extensions to string.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Updates a string and appends a suffix.
    /// </summary>
    /// <param name="value">The string to truncate.</param>
    /// <param name="maxChars">Cut after how many characters.</param>
    /// <param name="suffix">
    /// The suffix to append after cut, default: "…".
    /// Pass "string.empty" for no suffix.
    /// </param>
    /// <returns>The truncated string.</returns>
    public static string Truncate(this string value, int maxChars, string suffix = "…") => value.Length <= maxChars ? value : $"{value[..maxChars]}{suffix}";
}