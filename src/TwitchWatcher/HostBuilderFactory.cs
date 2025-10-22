using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using TwitchWatcher.Configuration;
using TwitchWatcher.Contracts;
using TwitchWatcher.Services;

namespace TwitchWatcher.Core
{
    public static class HostBuilderFactory
    {
        public static IHostBuilder CreateHostBuilder(string basePath, Action<HostBuilderContext, IServiceCollection>? configureExtras = null,
            Action<HostBuilderContext, IConfigurationBuilder>? configureConfigExtras = null)
        {
            return Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((ctx, cfg) =>
                {
                    cfg.SetBasePath(basePath);
                    cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    cfg.AddJsonFile("channels.json", optional: true, reloadOnChange: true);
                    cfg.AddEnvironmentVariables();

                    configureConfigExtras?.Invoke(ctx, cfg);
                })
                .ConfigureServices((ctx, services) =>
                {
                    services.Configure<AppOptions>(ctx.Configuration.GetSection("App"));
                    services.AddHttpClient("TwitchHelix", c => c.BaseAddress = new Uri("https://api.twitch.tv/helix/"));
                    services.AddSingleton<ITwitchAuthService, TwitchAuthService>();
                    services.AddSingleton<ITwitchApi, TwitchApi>();
                    services.AddSingleton<IPlayerFactory, PlayerFactory>();
                    services.AddHostedService<MultiChannelWatcher>();

                    configureExtras?.Invoke(ctx, services);
                });
        }

        public static IHost BuildHost(string basePath) => CreateHostBuilder(basePath).Build();
    }
}
