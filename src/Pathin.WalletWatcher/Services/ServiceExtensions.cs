// <copyright file="ServiceExtensions.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Pathin.WalletWatcher.Attributes;

namespace Pathin.WalletWatcher.Services;

/// <summary>
/// The ServiceExtensions class provides extension methods for the <see cref="IServiceCollection"/> interface.
/// These extension methods help in registering services that implement the <see cref="IAppService"/> interface.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Registers all services that implement the <see cref="IAppService"/> interface using the appropriate
    /// service lifetime based on their attribute. This method searches for types with the <see cref="TransientServiceAttribute"/>,
    /// <see cref="ScopedServiceAttribute"/>, and <see cref="SingletonServiceAttribute"/> attributes and registers them accordingly.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> instance where the services will be registered.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance with the registered services.</returns>
    public static IServiceCollection RegisterApplicationServices(this IServiceCollection services)
    {
        var serviceTypes = typeof(ServiceExtensions).Assembly.GetTypes().Where(t => t.GetInterfaces().Any(i => i == typeof(IAppService)));

        foreach (var serviceType in serviceTypes)
        {
            var serviceInterface = typeof(IAppService);
            if (serviceType.GetCustomAttribute<TransientServiceAttribute>() != null)
            {
                services.AddTransient(serviceInterface, serviceType);
            }
            else if (serviceType.GetCustomAttribute<ScopedServiceAttribute>() != null)
            {
                services.AddScoped(serviceInterface, serviceType);
            }
            else if (serviceType.GetCustomAttribute<SingletonServiceAttribute>() != null)
            {
                services.AddSingleton(serviceInterface, serviceType);
            }
        }

        return services;
    }
}