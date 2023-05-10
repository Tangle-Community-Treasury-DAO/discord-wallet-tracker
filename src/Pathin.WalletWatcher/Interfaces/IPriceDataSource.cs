// <copyright file="IPriceDataSource.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

namespace Pathin.WalletWatcher.Services.PriceData.Source;

/// <summary>
/// Interface for the price data source implementations.
/// </summary>
public interface IPriceDataSource
{
    /// <summary>
    /// Gets the source name.
    /// For example CoinGecko or CoinMarketCap.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the source's primary link.
    /// </summary>
    public string? Link { get; }

    /// <summary>
    /// Gets all of the endpoint's available tokens.
    /// </summary>
    /// <returns>A list of endpoint available tokens.</returns>
    public Task<ICollection<SourceTokenInfo>> GetTokensAsync();

    /// <summary>
    /// Gets the price data for a token.
    /// </summary>
    /// <param name="tokenInfo">The token info.</param>
    /// <param name="priceCurrency">The currency to get the prices in. Default = USD.</param>
    /// <returns>The token's price data.</returns>
    public Task<TokenPriceData> GetPriceForTokenAsync(SourceTokenInfo tokenInfo, string priceCurrency = "USD");

    /// <summary>
    /// Gets a link for a respective token.
    /// Null if the upstream source has no link.
    /// </summary>
    /// <param name="tokenInfo">The token.</param>
    /// <returns>The token link.</returns>
    public string? GetTokenLink(SourceTokenInfo tokenInfo);
}