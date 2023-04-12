// <copyright file="AppSettings.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

namespace Pathin.WalletWatcher.Config;

/// <summary>
/// The AppSettings class represents the application settings.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Gets the EVM endpoints.
    /// </summary>
    public List<EvmEndpointConfig> EvmEndpoints { get; init; } = new List<EvmEndpointConfig>();

    /// <summary>
    /// Gets the wallets.
    /// </summary>
    public List<WalletConfig> Wallets { get; init; } = new List<WalletConfig>();

    /// <summary>
    /// Gets the Discord webhook URLs.
    /// </summary>
    /// <param name="wallet">The wallet.</param>
    /// <returns>A list of <see cref="EvmEndpointConfig"/> for the provided <paramref name="wallet"/>.</returns>
    public IEnumerable<EvmEndpointConfig> GetEvmEndpointConfigsForWallet(WalletConfig wallet)
    {
        return EvmEndpoints.Where(e => wallet.EvmEndpoints.Contains(e.Name));
    }
}