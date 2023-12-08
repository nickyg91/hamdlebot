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
        var appConfigConnectionString = root["ConnectionStrings:AppConfig"];
        var tenantId = root["Hyperion:ManagedIdentity:TenantId"];
        var managedIdentityClientId = root["Hyperion:ManagedIdentity:ClientId"];

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