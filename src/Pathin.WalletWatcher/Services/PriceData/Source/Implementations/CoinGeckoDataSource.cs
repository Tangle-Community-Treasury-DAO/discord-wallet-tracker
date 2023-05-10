// <copyright file="CoinGeckoDataSource.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using CoinGecko.Clients;
using Newtonsoft.Json;
using Pathin.WalletWatcher.Extensions;

namespace Pathin.WalletWatcher.Services.PriceData.Source.Implementations;

/// <summary>
/// Implementation of <see cref="IPriceDataSource" /> for CoinGecko.
/// </summary>
public class CoinGeckoDataSource : IPriceDataSource
{
    private readonly CoinsClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoinGeckoDataSource"/> class.
    /// </summary>
    public CoinGeckoDataSource()
    {
        _client = new CoinsClient(new HttpClient(), new JsonSerializerSettings());
    }

    /// <inheritdoc/>
    public string Name => "CoinGecko";

    /// <inheritdoc/>
    public string? Link => "https://www.coingecko.com";

    /// <inheritdoc/>
    public async Task<TokenPriceData> GetPriceForTokenAsync(SourceTokenInfo tokenInfo, string priceCurrency = "USD")
    {
        var data = await _client.GetAllCoinDataWithId(tokenInfo.Id);

        var currencyKey = priceCurrency.ToLowerInvariant();
        var res = new TokenPriceData(tokenInfo, priceCurrency, this)
        {
            MarketCapRank = data.MarketCapRank?.ConvertTo<uint>(),
            CurrentPrice = data.MarketData.CurrentPrice.TryGetValue(currencyKey, out var parsedValueCurrent) ? parsedValueCurrent?.ConvertTo<decimal>() : null,
            HighestPrice24H = data.MarketData.High24H.TryGetValue(currencyKey, out var parsedValueHigh) ? parsedValueHigh?.ConvertTo<decimal>() : null,
            LowestPrice24H = data.MarketData.Low24H.TryGetValue(currencyKey, out var parsedValueLow) ? parsedValueLow?.ConvertTo<decimal>() : null,
            MarketCap = data.MarketData.MarketCap.TryGetValue(currencyKey, out var parsedMarketCap) ? parsedMarketCap?.ConvertTo<decimal>() : null,
            PriceChangePercentage1Hour = data.MarketData.PriceChangePercentage1HInCurrency.TryGetValue(currencyKey, out var parsedPriceChangePercentage1H) ? parsedPriceChangePercentage1H : null,
            PriceChangePercentage24Hours = data.MarketData.PriceChangePercentage24HInCurrency.TryGetValue(currencyKey, out var parsedPriceChangePercentage24H) ? parsedPriceChangePercentage24H : null,
            PriceChangePercentageAth = data.MarketData.AthChangePercentage.TryGetValue(currencyKey, out var parsedPriceChangePercentageAth) ? parsedPriceChangePercentageAth.ConvertTo<double>() : null,
            TotalVolume = data.MarketData.TotalVolume.TryGetValue(currencyKey, out var parsedTotalVolume) ? parsedTotalVolume?.ConvertTo<decimal>() : null,
            AthPrice = data.MarketData.Ath.TryGetValue(currencyKey, out var athPrice) ? athPrice?.ConvertTo<decimal>() : null,
            AthDate = data.MarketData.AthDate.TryGetValue(currencyKey, out var athDate) ? athDate : null,
        };

        return res;
    }

    /// <inheritdoc/>
    public string? GetTokenLink(SourceTokenInfo tokenInfo) => string.Format(Link + "/en/coins/{0}", tokenInfo.Id);

    /// <inheritdoc/>
    public async Task<ICollection<SourceTokenInfo>> GetTokensAsync()
    {
        var res = new List<SourceTokenInfo>();
        var tokens = await _client.GetCoinList();
        foreach (var token in tokens)
        {
            res.Add(new SourceTokenInfo(token.Id, token.Name, token.Symbol));
        }

        return res;
    }
}