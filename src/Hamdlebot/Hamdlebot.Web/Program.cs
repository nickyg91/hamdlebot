using Hamdle.Cache;
using Hamdlebot.Core;
using Hamdlebot.Core.Extensions;
using Hamdlebot.Core.Models;
using Hamdlebot.Core.SignalR.Clients.Logging;
using Hamdlebot.Data.Contexts.Hamdlebot;
using Hamdlebot.Data.Contexts.Hamdlebot.Repositories;
using Hamdlebot.Data.Contexts.Hamdlebot.Repositories.Interfaces;
using HamdleBot.Services;
using HamdleBot.Services.Handlers;
using HamdleBot.Services.OBS;
using HamdleBot.Services.Twitch;
using HamdleBot.Services.Twitch.Interfaces;
using Hamdlebot.Web.Hubs;
using Hamdlebot.Worker;
using MassTransit;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var isDevelopment = builder.Environment.IsDevelopment();
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddAzureAppConfig(isDevelopment);
builder.Services.AddSingleton<ICacheService, CacheService>();
var appSettings = builder.Configuration.GetSection("Settings");
builder.Services.Configure<AppConfigSettings>(appSettings);

var oauthHandler = new HttpClientHandler();
var oauthHttpClient = new HttpClient(oauthHandler);

var twitchApiHttpClient = new HttpClient();

var hamdlebotHubUrl = isDevelopment ? "https://localhost:7256/hamdlebothub" : "http://localhost:8080/hamdlebothub";
var botLogHubUrl = isDevelopment ? "https://localhost:7256/botloghub" : "http://localhost:8080/botloghub";
var connectionString = builder.Configuration.GetConnectionString("hamdlebot");

var hamdleBotHubConnection = new HubConnectionBuilder()
    .WithUrl(hamdlebotHubUrl)
    .WithAutomaticReconnect()
    .Build();

var botLogHubConnection = new HubConnectionBuilder()
    .WithUrl(botLogHubUrl)
    .WithAutomaticReconnect()
    .Build();

builder.Services.AddDbContext<HamdlebotContext>(options =>
{
    options.UseNpgsql(connectionString, npgBuilder =>
    {
        // For now.
        npgBuilder.MigrationsAssembly("Hamdlebot.Data");
    });
});

builder.Services.AddSingleton(oauthHttpClient);
builder.Services.AddSingleton<ITwitchChatService, TwitchChatService>();
builder.Services.AddSingleton<ITwitchEventSubService, TwitchEventSubService>();
builder.Services.AddSingleton<ITwitchIdentityApiService, TwitchIdentityApiService>();
builder.Services.AddSingleton<IObsService, ObsService>();
builder.Services.AddMassTransit(x =>
{
    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddSingleton<IWordService, WordService>();
builder.Services.AddSingleton<TwitchAuthTokenUpdateHandler>();
builder.Services.AddKeyedSingleton("twitchApiHttpClient", twitchApiHttpClient);
builder.Services.AddKeyedSingleton("logHub", botLogHubConnection);
builder.Services.AddKeyedSingleton("hamdleHub", hamdleBotHubConnection);
builder.Services.AddTransient<IBotLogClient, BotLogClient>();
builder.Services.AddScoped<IBotChannelRepository, BotChannelRepository>();
builder.Services.AddHostedService<HamdlebotWorker>();
builder.Services.AddAuthentication().AddJwtBearer(opt =>
{
    var settings = builder.Configuration.GetSection("Settings").Get<AppConfigSettings>();
    opt.IncludeErrorDetails = true;
    opt.MetadataAddress = "https://id.twitch.tv/oauth2/.well-known/openid-configuration";
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = "https://id.twitch.tv/oauth2",
        ValidAudience = settings?.TwitchConnectionInfo?.ClientId,
        ValidateAudience = true,
    };
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<IAuthenticatedTwitchUser, AuthenticatedTwitchUser>(provider =>
{
    var user =
        provider.GetService<IHttpContextAccessor>()!.HttpContext!.User;
    return new AuthenticatedTwitchUser(user);
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    RequireHeaderSymmetry = false
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<HamdlebotHub>("/hamdlebothub");
app.MapHub<BotLogHub>("/botloghub");
app.MapFallbackToFile("index.html");

// move to migration app at some point.
using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<HamdlebotContext>();
    ctx.Database.Migrate();
}

app.Run();