using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pathin.WalletWatcher.Config;

namespace Pathin.WalletWatcher;

public class Startup
{
    public IConfiguration Configuration { get; }
    public ILogger<Startup> Logger { get; }

    public Startup(IConfiguration configuration, ILogger<Startup> logger)
    {
        Configuration = configuration;
        Logger = logger;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
        services.AddTransient<App>();
    }
}

