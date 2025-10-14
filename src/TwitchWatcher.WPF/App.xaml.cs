
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.Marshalling;
using System.Windows;
using TwitchWatcher.Configuration;
using TwitchWatcher.Core;
using TwitchWatcher.Core.Contracts;
using TwitchWatcher.Services;
using TwitchWatcher.WPF.Infrastructure.Configuration;
using TwitchWatcher.WPF.ViewModels;

namespace TwitchWatcher.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IHost? AppHost { get; set; } = default!;

        public App()
        {
            AppHost = HostBuilderFactory.CreateHostBuilder(AppContext.BaseDirectory, (ctx, services) =>
            {
                services.Configure<AppOptions>(ctx.Configuration.GetSection("App"));
                services.AddSingleton<IWritableOptions<AppOptions>>(sp =>
                new JsonWritableOptions<AppOptions>(
                    (IConfigurationRoot)sp.GetRequiredService<IConfiguration>(),
                    "App",
                    Path.Combine(AppContext.BaseDirectory, "channels.json")));

                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
                services.AddSingleton<IChannelStateUpdater>(sp => sp.GetRequiredService<MainViewModel>());
            }).Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await AppHost.StartAsync();

            var win = AppHost.Services.GetRequiredService<MainWindow>();
            MainWindow = win;
            win.Show();
            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await AppHost.StopAsync();
            AppHost.Dispose();
            base.OnExit(e);
        }
    }
}
