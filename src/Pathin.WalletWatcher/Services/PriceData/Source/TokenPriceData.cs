// <copyright file="TokenPriceData.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

namespace Pathin.WalletWatcher.Services.PriceData.Source;

/// <summary>
/// Token price data.
/// </summary>
public class TokenPriceData
{
    private decimal? _marketCap;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenPriceData"/> class.
    /// </summary>
    /// <param name="tokenInfo">The token info.</param>
    /// <param name="currency">The token price currency.</param>
    /// <param name="dataSource">The data source.</param>
    public TokenPriceData(SourceTokenInfo tokenInfo, string currency, IPriceDataSource dataSource)
    {
        TokenInfo = tokenInfo;
        Currency = currency;
        DataSource = dataSource;
    }

    /// <summary>
    /// Gets the token info.
    /// </summary>
    public SourceTokenInfo TokenInfo { get; }

    /// <summary>
    /// Gets the token price currency.
    /// </summary>
    public string Currency { get; }

    /// <summary>
    /// Gets the data source.
    /// </summary>
    public IPriceDataSource DataSource { get; }

    /// <summary>
    /// Gets the asset rank.
    /// </summary>
    public uint? MarketCapRank { get; internal set; }

    /// <summary>
    /// Gets the current price.
    /// </summary>
    public decimal? CurrentPrice { get; internal set; }

    /// <summary>
    /// Gets the market cap.
    /// </summary>
    public decimal? MarketCap
    {
        get
        {
            return (_marketCap == decimal.Zero || _marketCap == null) && TokenInfo.Symbol.Equals("SMR", StringComparison.InvariantCultureIgnoreCase) && CurrentPrice.HasValue
                ? CurrentPrice.Value * 1813620509
                : _marketCap;
        }
        internal set => _marketCap = value;
    }

    /// <summary>
    /// Gets the price change in percentage in the last 24 hours.
    /// </summary>
    public double? PriceChangePercentage24Hours { get; internal set; }

    /// <summary>
    /// Gets the price change in percentage in the last hour.
    /// </summary>
    public double? PriceChangePercentage1Hour { get; internal set; }

    /// <summary>
    /// Gets the price change in percentage in the last hour since ATH.
    /// </summary>
    public double? PriceChangePercentageAth { get; internal set; }

    /// <summary>
    /// Gets the ATH price.
    /// </summary>
    public decimal? AthPrice { get; internal set; }

    /// <summary>
    /// Gets the ATH date.
    /// </summary>
    public DateTimeOffset? AthDate { get; internal set; }

    /// <summary>
    /// Gets the highest price.
    /// </summary>
    public decimal? HighestPrice24H { get; internal set; }

    /// <summary>
    /// Gets the lowest price.
    /// </summary>
    public decimal? LowestPrice24H { get; internal set; }

    /// <summary>
    /// Gets the total volume.
    /// </summary>
    public decimal? TotalVolume { get; internal set; }

    /// <summary>
    /// Gets the created date.
    /// </summary>
    public DateTime CreatedDate { get; } = DateTime.Now;
}