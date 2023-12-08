using Hamdle.Cache;
using Hamdlebot.Core;
using Hamdlebot.Core.Extensions;
using HamdleBot.Services;
using HamdleBot.Services.Mediators;
using HamdleBot.Services.OBS;
using HamdleBot.Services.Twitch;
using HamdleBot.Services.Twitch.Interfaces;
using Hamdlebot.TwitchServices.Api;
using Hamdlebot.Worker;
using Microsoft.AspNetCore.SignalR.Client;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((builder, config) =>
    {
        var settings = config.Build();
        config
            .AddEnvironmentVariables()
            .AddAzureAppConfig(settings, builder.HostingEnvironment.IsDevelopment());
    })
    .ConfigureServices((builder, services) =>
    {
        var appSettings = builder.Configuration.GetSection("Settings");
        services.Configure<AppConfigSettings>(appSettings);
        var oauthHandler = new HttpClientHandler();
        var oauthHttpClient = new HttpClient(oauthHandler);
        services.AddSingleton(oauthHttpClient);
        services.AddSingleton<ITwitchChatService, TwitchChatService>();
        services.AddSingleton<ITwitchIdentityApiService, TwitchIdentityApiService>();
        services.AddSingleton<ICacheService, CacheService>();
        services.AddSingleton<IWordService, WordService>();
        services.AddSingleton<HubConnection>((_) => new HubConnectionBuilder()
            .WithUrl("https://localhost:7256/hamdlebothub").Build());
        services.AddSingleton<IObsService, ObsService>();
        services.AddSingleton<IHamdleService, HamdleService>();
        services.AddSingleton<HamdleMediator>();
        services.AddHostedService<Worker>();
    })
    .Build();
host.Run();