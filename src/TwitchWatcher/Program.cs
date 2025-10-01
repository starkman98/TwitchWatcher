using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchWatcher.Configuration;
using TwitchWatcher.Contracts;
using TwitchWatcher.Services;
using static System.Net.WebRequestMethods;


var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { ContentRootPath = AppContext.BaseDirectory});

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets<Program>()   // pulls in what you set above
    .AddEnvironmentVariables();

builder.Logging.AddConsole();

builder.Services.Configure<AppOptions>(builder.Configuration.GetSection("App"));

//var opts = builder.Configuration.GetSection("App").Get<AppOptions>();
//Console.WriteLine($"ChannelName: {opts.ChannelName}");                    <= Kollar om ChannelName och ClientId har fått värden.
//Console.WriteLine($"ClientId length: {opts.ClientId?.Length}");

builder.Services.AddSingleton<ITwitchAuthService, TwitchAuthService>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var auth = scope.ServiceProvider.GetRequiredService<ITwitchAuthService>();
    var token = await auth.GetTokenAsync();
    Console.WriteLine($"Got token length: {token.Length}");
}

host.Run();