// <copyright file="App.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pathin.WalletWatcher.Services;

namespace Pathin.WalletWatcher;

/// <summary>
/// The main application class for the Pathin Wallet Watcher.
/// </summary>
public class App
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<App> _logger;
    private readonly Dictionary<string, Func<Task>> _commands;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <param name="logger">The logger for the App class.</param>
    public App(IServiceProvider serviceProvider, ILogger<App> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _commands = new Dictionary<string, Func<Task>>(StringComparer.OrdinalIgnoreCase)
        {
            { "start", StartServices },
            { "stop", StopServices },
            { "status", PrintServiceStatuses },
            { "exit", async () => await StopServices() },
        };
    }

    /// <summary>
    /// Runs the application, handling user input for managing services.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task RunAsync()
    {
        _logger.LogInformation("Starting services...");
        await StartServices();

        while (true)
        {
            _logger.LogInformation("Enter a command (Start, Stop, Status, or Exit):");
            string? command = Console.ReadLine()?.ToLower();

            if (command != null && _commands.TryGetValue(command, out var action))
            {
                await action();
            }
            else
            {
                _logger.LogWarning("Invalid command. Please enter Start, Stop, Status, or Exit.");
                Thread.Sleep(1000);
            }
        }
    }

    private async Task StartServices()
    {
        _logger.LogInformation("Starting services...");
        var services = _serviceProvider.GetServices<IAppService>();
        foreach (var service in services)
        {
            await service.StartAsync();
        }

        _logger.LogInformation("Services started.");
    }

    private async Task StopServices()
    {
        _logger.LogInformation("Stopping services...");
        var services = _serviceProvider.GetServices<IAppService>();
        foreach (var service in services)
        {
            await service.StopAsync();
        }

        _logger.LogInformation("Services stopped.");
    }

    private Task PrintServiceStatuses()
    {
        var services = _serviceProvider.GetServices<IAppService>();
        foreach (var service in services)
        {
            _logger.LogInformation("{ServiceName}: {ServiceStatus}", service.GetType().Name, service.GetStatus());
        }

        return Task.CompletedTask;
    }
}