using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hamdlebot.Core.Exceptions;
using Hamdlebot.Models;
using HamdleBot.Services.Twitch.Interfaces;

namespace Hamdlebot.TwitchServices.Api;

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
}