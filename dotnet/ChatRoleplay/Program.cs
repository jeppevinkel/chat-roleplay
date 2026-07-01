using ChatRoleplay.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .ConfigureServices((_, services) =>
    {
        // HTTP clients
        services.AddHttpClient();

        // Application services (singletons shared across the app)
        services.AddSingleton<ConfigService>();
        services.AddSingleton<CharacterBotService>();

        // Background service (the manager bot runs as a hosted service)
        services.AddHostedService<ManagerBotService>();
    });

var host = builder.Build();
await host.RunAsync();
