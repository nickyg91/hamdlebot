using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
namespace Hamdlebot.Core.Extensions;


public static class AppConfigServiceExtensions
{
    public static void AddAzureAppConfig(this IConfigurationBuilder configuration, IConfigurationRoot root, bool isDev)
    {
        var env = isDev ? "Dev" : "Prod";
        // DO NOT SHOW VALUES ON STREAM.
        var managedIdentityClientId = "";
        var appConfigConnectionString = "";
        var tenantId = "";

        if (isDev)
        {
            appConfigConnectionString = root["ConnectionStrings:AppConfig"];
            tenantId = root["Hyperion:ManagedIdentity:TenantId"];
            managedIdentityClientId = root["Hyperion:ManagedIdentity:ClientId"];
        }
        else
        {
            appConfigConnectionString = Environment.GetEnvironmentVariable("APPCONFIG_CONNECTIONSTRING");
            tenantId = Environment.GetEnvironmentVariable("AZURE_TENANTID");
            managedIdentityClientId = Environment.GetEnvironmentVariable("MANAGED_USER_CLIENTID");
        }

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
    }
}