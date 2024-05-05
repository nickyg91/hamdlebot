using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
namespace Hamdlebot.Core.Extensions;


public static class AppConfigServiceExtensions
{
    public static IConfigurationBuilder AddAzureAppConfig(this IConfigurationBuilder configuration, IConfigurationRoot root, bool isDev)
    {
        var env = isDev ? "Dev" : "Prod";
        // DO NOT SHOW VALUES ON STREAM.
        var appConfigConnectionString = isDev ? root["ConnectionStrings:AppConfig"] : Environment.GetEnvironmentVariable("ConnectionStrings__AppConfig");
        var tenantId = isDev ? root["Hyperion:ManagedIdentity:TenantId"] : Environment.GetEnvironmentVariable("Hyperion__ManagedIdentity__TenantId");
        var managedIdentityClientId = isDev ? root["Hyperion:ManagedIdentity:ClientId"] : Environment.GetEnvironmentVariable("Hyperion__ManagedIdentity__TenantId");

        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = managedIdentityClientId,
            TenantId = tenantId,
        });

        // AGAIN DONT SHOW THESE WHEN DEBUGGING!
        configuration.AddAzureAppConfiguration(options =>
        {
            options
                .Connect(appConfigConnectionString)
                .ConfigureKeyVault(kv => { kv.SetCredential(credential); })
                .Select(KeyFilter.Any)
                .Select(KeyFilter.Any, env);
        });
        return configuration;
    }
    
    public static ConfigurationManager AddAzureAppConfig(this ConfigurationManager configuration, bool isDev)
    {
        var env = isDev ? "Dev" : "Prod";
        // DO NOT SHOW VALUES ON STREAM.
        var appConfigConnectionString = isDev ? configuration["ConnectionStrings:AppConfig"] : Environment.GetEnvironmentVariable("ConnectionStrings__AppConfig");
        var tenantId = isDev ? configuration["Hyperion:ManagedIdentity:TenantId"] : Environment.GetEnvironmentVariable("Hyperion__ManagedIdentity__TenantId");
        var managedIdentityClientId = isDev ? configuration["Hyperion:ManagedIdentity:ClientId"] : Environment.GetEnvironmentVariable("Hyperion__ManagedIdentity__TenantId");

        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = managedIdentityClientId,
            TenantId = tenantId,
        });

        // AGAIN DONT SHOW THESE WHEN DEBUGGING!
        configuration.AddAzureAppConfiguration(options =>
        {
            options
                .Connect(appConfigConnectionString)
                .ConfigureKeyVault(kv => { kv.SetCredential(credential); })
                .Select(KeyFilter.Any)
                .Select(KeyFilter.Any, env);
        });
        return configuration;
    }
}