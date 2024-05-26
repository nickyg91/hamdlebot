using System.Net.Http.Headers;

namespace Hamdlebot.Core.HttpHandlers;

public class TwitchBearerAuthenticationHttpHandler : HttpClientHandler
{
    private readonly string _bearerToken;
    private readonly string _clientId;

    public TwitchBearerAuthenticationHttpHandler(string bearerToken, string clientId)
    {
        _bearerToken = bearerToken;
        _clientId = clientId;
    }
    
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
        request.Headers.Add("Client-Id", _clientId);
        return await base.SendAsync(request, cancellationToken);
    }
}