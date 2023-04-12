// <copyright file="Program.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pathin.WalletWatcher;
using Pathin.WalletWatcher.Config;
using Pathin.WalletWatcher.Services;
using Serilog;

var configurationBuilder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("APP_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddEnvironmentVariables();

var configuration = configurationBuilder.Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddSingleton<ILoggerFactory, LoggerFactory>();
services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
services.AddLogging(builder => builder.AddSerilog());
services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
services.AddTransient<App>();
services.RegisterApplicationServices();

using var serviceProvider = services.BuildServiceProvider();
await serviceProvider.GetRequiredService<App>().RunAsync();