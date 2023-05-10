// <copyright file="ValueTypeExtensions.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

namespace Pathin.WalletWatcher.Extensions;

/// <summary>
/// Extensions for value types.
/// </summary>
public static class ValueTypeExtensions
{
    /// <summary>
    /// Converts a nullable uint to a display string.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The display string or "N/A" if conversion was not successful.</returns>
    public static string GetDisplayString(this uint? value)
    {
        return value.GetDisplayStringInternal();
    }

    /// <summary>
    /// Converts a nullable float to a display string.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="format">The format string.</param>
    /// <returns>The display string or "N/A" if conversion was not successful.</returns>
    public static string GetDisplayString(this float? value, string? format = null)
    {
        return value.GetDisplayStringInternal(format);
    }

    /// <summary>
    /// Converts a float to a display string.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="format">The format string.</param>
    /// <returns>The display string or "N/A" if conversion was not successful.</returns>
    public static string GetDisplayString(this float value, string? format = null)
    {
        return value.GetDisplayStringInternal(format);
    }

    /// <summary>
    /// Converts a nullable decimal to a display string.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="format">The format string.</param>
    /// <returns>The display string or "N/A" if conversion was not successful.</returns>
    public static string GetDisplayString(this decimal? value, string? format = null)
    {
        return value.GetDisplayStringInternal(format);
    }

    /// <summary>
    /// Converts a decimal to a display string.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="format">The format string.</param>
    /// <returns>The display string or "N/A" if conversion was not successful.</returns>
    public static string GetDisplayString(this decimal value, string? format = null)
    {
        return value.GetDisplayStringInternal(format);
    }

    /// <summary>
    /// Converts a nullable double to a display string.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="format">The format string.</param>
    /// <returns>The display string or "N/A" if conversion was not successful.</returns>
    public static string GetDisplayString(this double? value, string? format = null)
    {
        return value.GetDisplayStringInternal(format);
    }

    /// <summary>
    /// Converts a double to a display string.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="format">The format string.</param>
    /// <returns>The display string or "N/A" if conversion was not successful.</returns>
    public static string GetDisplayString(this double value, string? format = null)
    {
        return value.GetDisplayStringInternal(format);
    }

    /// <summary>
    /// Converts a nullable DateTimeOffset to a display string.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The display string or "N/A" if conversion was not successful.</returns>
    public static string GetDisplayString(this DateTimeOffset? value)
    {
        return value.GetDisplayStringInternal();
    }

    /// <summary>
    /// Converts a DateTimeOffset to a display string.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The display string or "N/A" if conversion was not successful.</returns>
    public static string GetDisplayString(this DateTimeOffset value)
    {
        return value.GetDisplayStringInternal();
    }

    private static string GetDisplayStringInternal(this object? value, string? format = null)
    {
        if (value == null)
        {
            return "N/A";
        }

        Type? type = value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(Nullable<>)
            ? Nullable.GetUnderlyingType(value.GetType())
            : value.GetType();
        return type == typeof(float) || type == typeof(decimal) || type == typeof(double)
            ? Convert.ToDecimal(value).ToString(format)
            : type == typeof(DateTimeOffset) && format == null
            ? ((DateTimeOffset)value).LocalDateTime.ToUniversalTime().ToLongDateString()
            : Convert.ToString(value)?.ToString() ?? "N/A";
    }
}