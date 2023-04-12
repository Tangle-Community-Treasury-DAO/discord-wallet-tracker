// <copyright file="WalletConfig.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

namespace Pathin.WalletWatcher.Config;

/// <summary>
/// The WalletConfig class represents the configuration for a wallet.
/// </summary>
public class WalletConfig
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WalletConfig"/> class.
    /// </summary>
    /// <param name="name">The wallet name.</param>
    /// <param name="address">The wallet address.</param>
    /// <param name="webhookUrl">The discord webhook URL.</param>
    public WalletConfig(string name, string address, string webhookUrl)
    {
        Name = name;
        Address = address;
        WebhookUrl = webhookUrl;
    }

    /// <summary>
    /// Gets the wallet name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the wallet address.
    /// </summary>
    public string Address { get; }

    /// <summary>
    /// Gets the discord webhook URL.
    /// </summary>
    public string WebhookUrl { get; }

    /// <summary>
    /// Gets the EVM endpoint names.
    /// </summary>
    public IEnumerable<string> EvmEndpoints { get; init; } = new List<string>();

    /// <summary>
    /// Gets a value indicating whether or not the wallet is active.
    /// </summary>
    public bool Active { get; init; }
}