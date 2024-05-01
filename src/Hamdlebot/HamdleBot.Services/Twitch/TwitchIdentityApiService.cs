using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Hamdlebot.Core;
using Hamdlebot.Core.Exceptions;
using Hamdlebot.Models;
using HamdleBot.Services.Twitch.Interfaces;
using Microsoft.Extensions.Options;

namespace HamdleBot.Services.Twitch;

public class TwitchIdentityApiService : ITwitchIdentityApiService
{
    private readonly HttpClient _client;
    private readonly AppConfigSettings _settings;
    public TwitchIdentityApiService(HttpClient client, IOptions<AppConfigSettings> settings)
    {
        _client = client;
        _settings = settings.Value;
    }

    public async Task<ClientCredentialsTokenResponse> GetToken(string code)
    {
        _client.DefaultRequestHeaders.Clear();
        using var content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
        {
            new("client_id", _settings.TwitchConnectionInfo!.ClientId!),
            new("client_secret", _settings.TwitchConnectionInfo!.ClientSecret!),
            new("grant_type", "authorization_code"),
            new("code", code),
            new("redirect_uri", _settings.TwitchConnectionInfo!.ClientRedirectUrl!)
        });
        var response = await _client.PostAsync(new Uri("https://id.twitch.tv/oauth2/token"), content);
        var json = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode && !string.IsNullOrEmpty(json))
        {
            var responseObject = JsonSerializer.Deserialize<ClientCredentialsTokenResponse>(json);
            if (responseObject == null)
            {
                throw new TokenGenerationException("An error occurred while generating a twitch token.");
            }
            return responseObject;
        }
        throw new TokenGenerationException(
            $"An error occurred while generating a twitch token: {response.StatusCode}.");
    }

    public async Task<ClientCredentialsTokenResponse> GetTokenForBot(string code)
    {
        if (code == null)
        {
            throw new Exception("No auth code found");
        }

        var response = await _client.PostAsync("https://id.twitch.tv/oauth2/token", new FormUrlEncodedContent(
            new List<KeyValuePair<string, string>>
            {
                new("client_id", _settings.TwitchConnectionInfo!.ClientId!),
                new("client_secret", _settings.TwitchConnectionInfo!.ClientSecret!),
                new("code", code),
                new("grant_type", "authorization_code"),
                new("redirect_uri", _settings.TwitchConnectionInfo!.WorkerRedirectUrl!)
            }));

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new TokenGenerationException(
                $"An error occurred while generating a twitch token: {response.StatusCode}.", new Exception(content));
        }
        var json = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<ClientCredentialsTokenResponse>(json);
        if (responseObject == null)
        {
            throw new TokenGenerationException("An error occurred while generating a twitch token.");
        }
        return responseObject;
    }

    public async Task<ClientCredentialsTokenResponse> RefreshToken(string refreshToken)
    {
        using var content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
        {
            new("client_id", _settings.TwitchConnectionInfo!.ClientId!),
            new("client_secret", _settings.TwitchConnectionInfo!.ClientSecret!),
            new("grant_type", "refresh_token"),
            new("refresh_token", refreshToken),
        });
        var response = await _client.PostAsync(new Uri("https://id.twitch.tv/oauth2/token"), content);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode || string.IsNullOrEmpty(json))
        {
            throw new TokenGenerationException(
                $"An error occurred while generating a twitch token: {response.StatusCode}.");

        }
        var responseObject = JsonSerializer.Deserialize<ClientCredentialsTokenResponse>(json);
        if (responseObject == null)
        {
            throw new TokenGenerationException("An error occurred while generating a twitch token.");
        }
        return responseObject;
    }

    public async Task<string?> ListenForRedirect(string redirectUrl)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add(_settings.TwitchConnectionInfo!.WorkerRedirectUrl!);
        listener.Prefixes.Add("http://localhost:3000/");
        listener.Start();
        var code = await OnRequest(listener);
        listener.Stop();
        return code;
    }

    public string GetWorkerAuthorizationCodeUrl()
    {
        return
            $"https://id.twitch.tv/oauth2/authorize?response_type=code&client_id={_settings.TwitchConnectionInfo!.ClientId}&redirect_uri={_settings.TwitchConnectionInfo.WorkerRedirectUrl}&scope=chat%3Aread+chat%3Aedit";
    }

    public string GetClientOIDCAuthorizationCodeUrl()
    {
        var byteArray = new byte[20];
        using (var random = RandomNumberGenerator.Create())
        {
            random.GetBytes(byteArray);
        }

        var nonce = Convert.ToBase64String(byteArray);
        var url =
            $"https://id.twitch.tv/oauth2/authorize?response_type=code&client_id={_settings.TwitchConnectionInfo!.ClientId}&redirect_uri={_settings.TwitchConnectionInfo.ClientRedirectUrl}&scope=channel%3Amanage%3Apolls+channel%3Aread%3Apolls+openid+user%3Aread%3Aemail&claims={{\"id_token\":{{\"email\":null,\"email_verified\":null}},\"userinfo\":{{\"email\":null,\"email_verified\":null,\"picture\":null,\"updated_at\":null}}}}&state={nonce}&nonce={nonce}";
        return url;
    }

    private async Task<string?> OnRequest(HttpListener listener)
    {
        while (listener.IsListening)
        {
            var context = await listener.GetContextAsync();
            var request = context.Request;
            var response = context.Response;
            await using var writer = new StreamWriter(response.OutputStream);
            if (request.QueryString.AllKeys.Any("code".Contains!))
            {
                return request.QueryString["code"];
            }
        }

        return null;
    }
}