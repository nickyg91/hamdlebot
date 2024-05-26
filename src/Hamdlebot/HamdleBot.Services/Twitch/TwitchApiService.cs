using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using Hamdle.Cache;
using Hamdlebot.Core.HttpHandlers;
using Hamdlebot.Models.Twitch;
using HamdleBot.Services.Twitch.Interfaces;

namespace HamdleBot.Services.Twitch;

public class TwitchApiService : ITwitchApiService
{
    private readonly HttpClient _httpClient;
    private readonly CancellationToken _cancellationToken;
    public TwitchApiService(string authorizationToken, string clientId, CancellationToken cancellationToken)
    {
        _httpClient = new HttpClient(new TwitchBearerAuthenticationHttpHandler(authorizationToken, clientId))
        {
            BaseAddress = new Uri("https://api.twitch.tv/helix/"),
        };
        _cancellationToken = cancellationToken;
    }
    
    public async Task<TwitchApiResponse<GetUsersResponse>?> GetUsersByLogin(List<string> users)
    {
        var queryString = string.Join("&", users.Select(x => $"login={HttpUtility.UrlEncode(x)}"));
        var uri = new Uri($"users?{queryString}", UriKind.Relative);
        var response = await _httpClient.GetAsync(uri, _cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            // custom exception here
            throw new Exception("Failed to get users by login");
        }

        var content = await response.Content.ReadAsStringAsync(_cancellationToken);
        return JsonSerializer.Deserialize<TwitchApiResponse<GetUsersResponse>>(content);
    }

    public async Task<TwitchApiResponse<EventSubResponse>?> SubscribeToEvents(EventSubRequest request)
    {
        var uri = new Uri("eventsub/subscriptions", UriKind.Relative);
        var response = await _httpClient.PostAsJsonAsync(uri, request, _cancellationToken);
        var content = await response.Content.ReadAsStringAsync(_cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            // custom exception here
            throw new Exception("Failed to subscribe to events.");
        }
        return JsonSerializer.Deserialize<TwitchApiResponse<EventSubResponse>>(content);
    }
}