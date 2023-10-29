using Hamdle.Cache;
using Hamdlebot.Core;
using Hamdlebot.Core.Extensions;
using HamdleBot.Services;
using Hamdlebot.TwitchServices.Api;
using Hamdlebot.TwitchServices.Interfaces;
using Hamdlebot.Worker;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((builder, config) =>
    {
        config.AddEnvironmentVariables();
        var settings = config.Build();
        config.AddAzureAppConfig(settings, builder.HostingEnvironment.IsDevelopment());
        //var settings = builder.Configuration.GetSection("Settings");
    })
    .ConfigureServices((builder, services) =>
    {
        var appSettings = builder.Configuration.GetSection("Settings");
        services.Configure <AppConfigSettings>(appSettings);
        var oauthHandler = new HttpClientHandler();
        var oauthHttpClient = new HttpClient(oauthHandler);
        
        services.AddSingleton(oauthHttpClient);
        services.AddSingleton<ITwitchChatService, TwitchChatService>();
        services.AddSingleton<ITwitchIdentityApiService, TwitchIdentityApiService>();
        services.AddSingleton<ICacheService, CacheService>();
        services.AddSingleton<IHamdleWordService, HamdleWordService>();
        services.AddHostedService<Worker>();
    })
    .Build();
host.Run();