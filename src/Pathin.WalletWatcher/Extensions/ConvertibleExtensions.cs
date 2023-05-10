// <copyright file="ConvertibleExtensions.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

namespace Pathin.WalletWatcher.Extensions;

/// <summary>
/// Converter extensions.
/// </summary>
public static class ConvertibleExtensions
{
    /// <summary>
    /// Converts one type to another.
    /// </summary>
    /// <param name="obj">The object to convert.</param>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <returns>The converted value or null.</returns>
    public static T? ConvertTo<T>(this IConvertible obj)
    {
        var t = typeof(T);

        return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>)
            ? obj == null ? (T?)(object?)null : (T)Convert.ChangeType(obj, Nullable.GetUnderlyingType(t)!)
            : (T)Convert.ChangeType(obj, t);
    }
}