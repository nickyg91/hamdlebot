using System.Net;
using System.Text.Json;
using Hamdlebot.Core.Exceptions;
using Hamdlebot.Models;
using HamdleBot.Services.Twitch.Interfaces;

namespace HamdleBot.Services.Twitch;

public class TwitchIdentityApiService : ITwitchIdentityApiService
{
    private readonly HttpClient _client;

    public TwitchIdentityApiService(HttpClient client)
    {
        _client = client;
    }

    public async Task<ClientCredentialsTokenResponse?> GetToken(ClientCredentialsTokenRequest request)
    {
        _client.DefaultRequestHeaders.Clear();
        using var content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
        {
            new("client_id", request.ClientId),
            new("client_secret", request.ClientSecret),
            new("grant_type", "client_credentials"),
        });
        var response = await _client.PostAsync(new Uri("https://id.twitch.tv/oauth2/token"), content);
        var json = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode && !string.IsNullOrEmpty(json))
        {
            var responseObject = JsonSerializer.Deserialize<ClientCredentialsTokenResponse>(json);
            return responseObject;
        }

        throw new TokenGenerationException(
            $"An error occurred while generating a twitch token: {response.StatusCode}.");
    }

    public async Task<ClientCredentialsTokenResponse?> GetTokenFromCodeFlow(ClientCredentialsTokenRequest request)
    {
        var code = await ListenForRedirect("http://localhost:3000");
        if (code == null)
        {
            throw new Exception("No auth code found");
        }

        var response = await _client.PostAsync("https://id.twitch.tv/oauth2/token", new FormUrlEncodedContent(
            new List<KeyValuePair<string, string>>
            {
                new("client_id", request.ClientId),
                new("client_secret", request.ClientSecret),
                new("code", code),
                new("grant_type", "authorization_code"),
                new("redirect_uri", "http://localhost:3000")
            }));

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        var json = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<ClientCredentialsTokenResponse>(json);
        return responseObject;
    }

    public async Task<ClientCredentialsTokenResponse?> RefreshToken(ClientCredentialsTokenRequest request)
    {
        using var content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
        {
            new("client_id", request.ClientId),
            new("client_secret", request.ClientSecret),
            new("grant_type", "refresh_token"),
            new("refresh_token", request.RefreshToken),
        });
        var response = await _client.PostAsync(new Uri("https://id.twitch.tv/oauth2/token"), content);
        var json = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode && !string.IsNullOrEmpty(json))
        {
            var responseObject = JsonSerializer.Deserialize<ClientCredentialsTokenResponse>(json);
            return responseObject;
        }

        throw new TokenGenerationException(
            $"An error occurred while generating a twitch token: {response.StatusCode}.");
    }

    public async Task<string?> ListenForRedirect(string redirectUrl)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:3000/");
        listener.Start();
        var code = await OnRequest(listener);
        listener.Stop();
        return code;
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