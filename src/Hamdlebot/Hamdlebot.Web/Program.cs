using Hamdle.Cache;
using Hamdlebot.Core;
using Hamdlebot.Core.Extensions;
using Hamdlebot.Core.SignalR.Clients.Hamdle;
using Hamdlebot.Core.SignalR.Clients.Logging;
using HamdleBot.Services;
using HamdleBot.Services.OBS;
using HamdleBot.Services.Twitch;
using HamdleBot.Services.Twitch.Interfaces;
using Hamdlebot.Web.Hubs;
using Hamdlebot.Worker;
using Microsoft.AspNetCore.SignalR.Client;
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

var hamdlebotHubUrl = isDevelopment ? "https://localhost:7256/hamdlebothub" : "http://localhost:80/hamdlebothub";
var botLogHubUrl = isDevelopment ? "https://localhost:7256/botloghub" : "http://localhost:80/botloghub";

var hamdleBotHubConnection = new HubConnectionBuilder()
    .WithUrl(hamdlebotHubUrl)
    .WithAutomaticReconnect()
    .Build();

var botLogHubConnection = new HubConnectionBuilder()
    .WithUrl(botLogHubUrl)
    .WithAutomaticReconnect()
    .Build();

builder.Services.AddSingleton(oauthHttpClient);
builder.Services.AddSingleton<ITwitchChatService, TwitchChatService>();
builder.Services.AddSingleton<ITwitchIdentityApiService, TwitchIdentityApiService>();
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddSingleton<IWordService, WordService>();
builder.Services.AddTransient<IHamdleHubClient, HamdleHubClient>();
builder.Services.AddTransient<IBotLogClient, BotLogClient>();
builder.Services.AddSingleton<IObsService, ObsService>();
builder.Services.AddSingleton<IHamdleService, HamdleService>();
builder.Services.AddKeyedSingleton("logHub", botLogHubConnection);
builder.Services.AddKeyedSingleton("hamdleHub", hamdleBotHubConnection);
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

builder.Services.AddAuthorization();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<HamdlebotHub>("/hamdlebothub");
app.MapHub<BotLogHub>("/botloghub");
app.MapFallbackToFile("index.html");

app.Run();