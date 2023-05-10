// <copyright file="PriceDataService.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using System.Reflection.Metadata.Ecma335;
using ICCD.UltimatePriceBot.App.Models;
using Pathin.WalletWatcher.Services.PriceData.Source;

namespace Pathin.WalletWatcher.Services.PriceData;

/// <summary>
/// Gets price data from CoinGecko.
/// </summary>
public class PriceDataService
{
    private readonly Dictionary<string, TokenPriceData> _tokenPriceCache = new();

    private readonly Dictionary<string, SourceTokenInfo> _tokenLookupTable = new(StringComparer.InvariantCultureIgnoreCase);

    private readonly ReentrantAsyncLock.ReentrantAsyncLock _getPriceLock = new();

    private readonly IPriceDataSource _priceDataSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="PriceDataService"/> class.
    /// </summary>
    /// <param name="priceDataSource">The price data source implementation.</param>
    public PriceDataService(IPriceDataSource priceDataSource)
    {
        _priceDataSource = priceDataSource;
        Init().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the price data for a token.
    /// </summary>
    /// <param name="tokenKey">The token key (id, name, symbol or slug).</param>
    /// <param name="relations">TODO: The relations to get to that token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<PriceDataViewModel> GetPriceDataAsync(string tokenKey, params string[] relations)
    {
        await using (await _getPriceLock.LockAsync(CancellationToken.None))
        {
            if (!TokenExists(tokenKey))
            {
                throw new ArgumentException("Token not found.", nameof(tokenKey));
            }

            var tokenInfoObj = _tokenLookupTable[tokenKey];
            var data = await GetTokenPriceDataAsync(tokenInfoObj);

            var viewModel = new PriceDataViewModel(data);

            foreach (var relation in relations)
            {
                if (!_tokenLookupTable.TryGetValue(relation, out var relationTokenInfo) || relationTokenInfo.Id.Equals(tokenInfoObj.Id))
                {
                    continue;
                }

                var otherData = await GetTokenPriceDataAsync(relationTokenInfo);
                var otherViewModel = new PriceDataViewModel(otherData);
                viewModel.Relations.Add(otherViewModel.Symbol.ToUpperInvariant(), viewModel.GetRelationValueTo(otherViewModel));
            }

            return viewModel;
        }
    }

    /// <summary>
    /// Checks wheter or not a token exists.
    /// </summary>
    /// <param name="tokenName">The token name.</param>
    /// <returns>Whether a token exists or not.</returns>
    public bool TokenExists(string tokenName) => _tokenLookupTable.ContainsKey(tokenName);

    /// <summary>
    /// Gets a token ID by name.
    /// </summary>
    /// <param name="tokenName">The token name.</param>
    /// <returns>The token ID.</returns>
    public string GetTokenId(string tokenName)
    {
        return !TokenExists(tokenName)
            ? throw new ArgumentException("The token doesn't exist.", nameof(tokenName))
            : _tokenLookupTable[tokenName].Id;
    }

    private async Task<TokenPriceData> GetTokenPriceDataAsync(SourceTokenInfo tokenInfo)
    {
        _ = _tokenPriceCache.TryGetValue(tokenInfo.Id, out var data);
        if (data == null || DateTime.Now - data.CreatedDate > TimeSpan.FromSeconds(60))
        {
            // Update cache.
            data = await _priceDataSource.GetPriceForTokenAsync(tokenInfo);
            _tokenPriceCache[tokenInfo.Id] = data;
        }

        return data;
    }

    private async Task Init()
    {
        var allTokenInfos = await _priceDataSource.GetTokensAsync();
        for (var i = 0; i < 4; i++)
        {
            foreach (var tokenInfo in allTokenInfos)
            {
                if (i == 3 && tokenInfo.Slug == null)
                {
                    continue;
                }

                _ = i switch
                {
                    0 => _tokenLookupTable.TryAdd(tokenInfo.Id, tokenInfo),
                    1 => _tokenLookupTable.TryAdd(tokenInfo.Symbol, tokenInfo),
                    2 => _tokenLookupTable.TryAdd(tokenInfo.Name, tokenInfo),
                    3 => _tokenLookupTable.TryAdd(tokenInfo.Slug!, tokenInfo),
                    _ => throw new NotImplementedException(),
                };
            }
        }
    }
}