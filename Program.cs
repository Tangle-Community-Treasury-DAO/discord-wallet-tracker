using Microsoft.Extensions.Configuration;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Pathin.WalletWatcher;

class Program
{
    public static IConfiguration Configuration { get; set; }

    static async Task Main(string[] args)
    {
        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("APP_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables();

        Configuration = configurationBuilder.Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(Configuration)
            .CreateLogger();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(Configuration);
        services.AddSingleton<ILoggerFactory, LoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.AddLogging(builder => builder.AddSerilog());

        var startup = new Startup(Configuration, services.BuildServiceProvider().GetRequiredService<ILogger<Startup>>());
        startup.ConfigureServices(services);

        using var serviceProvider = services.BuildServiceProvider();
        await serviceProvider.GetRequiredService<App>().RunAsync();
    }
}