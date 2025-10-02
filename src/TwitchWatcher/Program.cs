using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TwitchWatcher.Configuration;
using TwitchWatcher.Contracts;
using TwitchWatcher.Services;
using TwitchWatcher.Models;
using static System.Net.WebRequestMethods;


var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { ContentRootPath = AppContext.BaseDirectory});

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets<Program>()   // pulls in what you set above
    .AddEnvironmentVariables();

builder.Logging.AddConsole();

builder.Services.Configure<AppOptions>(builder.Configuration.GetSection("App"));

builder.Services.AddSingleton<ITwitchAuthService, TwitchAuthService>();

builder.Services.AddHttpClient("TwitchHelix", uri => { uri.BaseAddress = new Uri("https://api.twitch.tv/helix/"); });

builder.Services.AddSingleton<ITwitchApi, TwitchApi>();

builder.Services.AddSingleton<IPlayerService, PlayerService>();

builder.Services.AddHostedService<StreamWatcher>();



var host = builder.Build();

//using (var scope = host.Services.CreateScope())
//{
//    var auth = scope.ServiceProvider.GetRequiredService<ITwitchAuthService>();
//    var token = await auth.GetTokenAsync();
//    Console.WriteLine($"Got token length: {token.Length}");

//    var api = scope.ServiceProvider.GetRequiredService<ITwitchApi>();
//    var options = scope.ServiceProvider.GetRequiredService<IOptions<AppOptions>>().Value;

//    var userId = await api.GetUserIdAsync(options.ChannelName);
//    var live = await api.IsLiveAsync(userId);

//    Console.WriteLine($"User: {options.ChannelName}, Live? {live}");

//}

host.Run();