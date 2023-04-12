// <copyright file="IAppService.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

namespace Pathin.WalletWatcher.Services;

/// <summary>
/// The IAppService interface defines the contract for services in the application.
/// </summary>
public interface IAppService
{
    /// <summary>
    /// Starts the service asynchronously.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task StartAsync();

    /// <summary>
    /// Stops the service asynchronously.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task StopAsync();

    /// <summary>
    /// Gets the status of the service.
    /// </summary>
    /// <returns>A string representing the status of the service.</returns>
    string GetStatus();
}