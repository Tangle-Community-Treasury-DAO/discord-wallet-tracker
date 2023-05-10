// <copyright file="PriceDataViewModel.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoinGecko.Entities.Response.Coins;
using Discord;
using Pathin.WalletWatcher.Services.PriceData.Source;

namespace ICCD.UltimatePriceBot.App.Models;

/// <summary>
/// Token price data.
/// </summary>
public class PriceDataViewModel
{
    private decimal? _marketCapUsd;

    /// <summary>
    /// Initializes a new instance of the <see cref="PriceDataViewModel"/> class.
    /// </summary>
    /// <param name="data">The API data response.</param>
    public PriceDataViewModel(TokenPriceData data)
    {
        DataSource = data.DataSource;

        Name = data.TokenInfo.Name;
        Symbol = data.TokenInfo.Symbol;
        Rank = data.MarketCapRank;
        CurrentPriceUsd = data.CurrentPrice;
        HighestPriceUsd = data.HighestPrice24H;
        LowestPriceUsd = data.LowestPrice24H;
        MarketCapUsd = data.MarketCap;
        PriceChangePercentage1Hour = data.PriceChangePercentage1Hour;
        PriceChangePercentage24Hours = data.PriceChangePercentage24Hours;
        PriceChangePercentageAth = data.PriceChangePercentageAth;
        TotalVolumeUsd = data.TotalVolume;
        AthPriceUsd = data.AthPrice;
        AthDate = data.AthDate;
        TokenLink = data.DataSource.GetTokenLink(data.TokenInfo);
    }

    /// <summary>
    /// Gets a link to the token from the upstream source.
    /// </summary>
    public string? TokenLink { get; }

    /// <summary>
    /// Gets the asset name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the asset symbol.
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// Gets the asset rank.
    /// </summary>
    public uint? Rank { get; internal set; }

    /// <summary>
    /// Gets the current price in USD.
    /// </summary>
    public decimal? CurrentPriceUsd { get; internal set; }

    /// <summary>
    /// Gets the market cap in USD.
    /// </summary>
    public decimal? MarketCapUsd
    {
        get
        {
            return Symbol.Equals("SMR", StringComparison.InvariantCultureIgnoreCase) && _marketCapUsd == decimal.Zero && CurrentPriceUsd.HasValue
                ? CurrentPriceUsd.Value * 1813620509
                : _marketCapUsd;
        }
        internal set => _marketCapUsd = value;
    }

    /// <summary>
    /// Gets the price change in percentage in the last 24 hours in USD.
    /// </summary>
    public double? PriceChangePercentage24Hours { get; internal set; }

    /// <summary>
    /// Gets the price change in percentage in the last hour in USD.
    /// </summary>
    public double? PriceChangePercentage1Hour { get; internal set; }

    /// <summary>
    /// Gets the price change in percentage in the last hour since ATH.
    /// </summary>
    public double? PriceChangePercentageAth { get; internal set; }

    /// <summary>
    /// Gets the ATH price.
    /// </summary>
    public decimal? AthPriceUsd { get; internal set; }

    /// <summary>
    /// Gets the ATH date.
    /// </summary>
    public DateTimeOffset? AthDate { get; internal set; }

    /// <summary>
    /// Gets the highest price.
    /// </summary>
    public decimal? HighestPriceUsd { get; internal set; }

    /// <summary>
    /// Gets the lowest price.
    /// </summary>
    public decimal? LowestPriceUsd { get; internal set; }

    /// <summary>
    /// Gets the total volume in USD.
    /// </summary>
    public decimal? TotalVolumeUsd { get; internal set; }

    /// <summary>
    /// Gets the relations to other tokens.
    /// </summary>
    public IDictionary<string, decimal?> Relations { get; } = new Dictionary<string, decimal?>();

    /// <summary>
    /// Gets the created date.
    /// </summary>
    public DateTime CreatedDate { get; } = DateTime.Now;

    /// <summary>
    /// Gets the data source.
    /// </summary>
    public IPriceDataSource DataSource { get; }

    /// <summary>
    /// Gets the relation to another price view model.
    /// </summary>
    /// <param name="other">The other view mode.</param>
    /// <returns>The value or null if the value could not be calculated.</returns>
    public decimal? GetRelationValueTo(PriceDataViewModel other)
    {
        var currentPriceDecimal = Convert.ToDecimal(CurrentPriceUsd);
        var otherPriceDecimal = Convert.ToDecimal(other.CurrentPriceUsd);

        decimal? relationValue = currentPriceDecimal != decimal.Zero && otherPriceDecimal != decimal.Zero ? currentPriceDecimal / otherPriceDecimal : null;
        return relationValue;
    }
}