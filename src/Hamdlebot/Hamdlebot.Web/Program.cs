using Hamdle.Cache;
using Hamdlebot.Core;
using Hamdlebot.Core.Extensions;
using Hamdlebot.Web.Hubs;

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


app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapHub<HamdlebotHub>("/hamdlebothub");
app.MapFallbackToFile("index.html");

app.Run();