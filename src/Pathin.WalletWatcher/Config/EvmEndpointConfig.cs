// <copyright file="EvmEndpointConfig.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

namespace Pathin.WalletWatcher.Config;

/// <summary>
/// The EvmEndpointConfig class represents the configuration for an Ethereum Virtual Machine (EVM) endpoint.
/// </summary>
/// <param name="Name">The name of the EVM endpoint.</param>
/// <param name="RpcUrl">The RPC URL of the EVM endpoint.</param>
/// <param name="NativeToken">The native token of the EVM endpoint.</param>
/// <param name="ExplorerUrl">The explorer URL of the EVM endpoint.</param>
public record EvmEndpointConfig(string Name, string RpcUrl, string NativeToken, string? ExplorerUrl = null);