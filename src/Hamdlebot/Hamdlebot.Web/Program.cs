using Hamdle.Cache;
using Hamdlebot.Core;
using Hamdlebot.Core.Extensions;
using Hamdlebot.Core.SignalR.Clients.Hamdle;
using Hamdlebot.Core.SignalR.Clients.Logging;
using HamdleBot.Services;
using HamdleBot.Services.Mediators;
using HamdleBot.Services.OBS;
using HamdleBot.Services.Twitch;
using HamdleBot.Services.Twitch.Interfaces;
using Hamdlebot.Web.Hubs;
using Hamdlebot.Worker;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
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

var hamdleBotHubConnection = new HubConnectionBuilder()
    .WithUrl("https://localhost:7256/hamdlebothub")
    .WithAutomaticReconnect()
    .Build();

var botLogHubConnection = new HubConnectionBuilder()
    .WithUrl("https://localhost:7256/botloghub")
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
builder.Services.AddSingleton<HamdleMediator>();
builder.Services.AddKeyedSingleton("logHub", botLogHubConnection);
builder.Services.AddKeyedSingleton("hamdleHub", hamdleBotHubConnection);
//builder.Services.AddHostedService<HamdlebotWorker>();

builder.Services.AddAuthentication().AddJwtBearer(opt =>
{
    opt.Authority = "https://id.twitch.tv/oauth2";
    opt.Configuration = new OpenIdConnectConfiguration
    {
        Issuer = "https://id.twitch.tv/oauth2",
        TokenEndpoint = "https://id.twitch.tv/oauth2/token",
        JwksUri = "https://id.twitch.tv/oauth2/keys",
        ClaimsParameterSupported = true,
        AuthorizationEndpoint = "https://id.twitch.tv/oauth2/authorize",
        IdTokenEncryptionAlgValuesSupported =
        {
            "RS256"
        },
        ClaimsSupported =
        {
            "email",
            "email_verified",
            "picture",
            "preferred_username",
            "exp",
            "iss",
            "sub",
            "azp",
            "aud",
            "iat",
            "updated_at"
        },
        ResponseTypesSupported =
        {
            "id_token",
            "code",
            "token",
            "code id_token",
            "token id_token"
        },
        ScopesSupported =
        {
            "openid"
        },
        SubjectTypesSupported =
        {
            "public"
        },
        TokenEndpointAuthMethodsSupported =
        {
            "client_secret_post"
        },
        UserInfoEndpoint = "https://id.twitch.tv/oauth2/userinfo",
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapHub<HamdlebotHub>("/hamdlebothub");
app.MapHub<BotLogHub>("/botloghub");
app.MapFallbackToFile("index.html");

app.Run();