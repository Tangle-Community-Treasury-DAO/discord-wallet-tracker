// <copyright file="SourceTokenInfo.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

namespace Pathin.WalletWatcher.Services.PriceData.Source;

/// <summary>
/// A source token.
/// </summary>
public class SourceTokenInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceTokenInfo"/> class.
    /// </summary>
    /// <param name="id">The token ID.</param>
    /// <param name="name">The token name.</param>
    /// <param name="symbol">The token symbol.</param>
    /// <param name="slug">The token slug.</param>
    public SourceTokenInfo(string id, string name, string symbol, string? slug = null)
    {
        Id = id;
        Name = name;
        Symbol = symbol;
        Slug = slug;
    }

    /// <summary>
    /// Gets the ID.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the symbol.
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// Gets the slug.
    /// </summary>
    public string? Slug { get; }
}