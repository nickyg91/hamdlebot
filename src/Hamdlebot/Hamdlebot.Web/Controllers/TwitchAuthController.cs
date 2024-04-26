using Hamdlebot.Models;
using HamdleBot.Services.Twitch.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Hamdlebot.Web.Controllers
{
    [Route("api/twitch/auth")]
    [ApiController]
    public class TwitchAuthController : ControllerBase
    {
        private readonly ITwitchIdentityApiService _twitchIdentityApiService;

        public TwitchAuthController(
            ITwitchIdentityApiService twitchIdentityApiService)
        {
            _twitchIdentityApiService = twitchIdentityApiService;
        }

        [HttpGet]
        public string Get()
        {
            return _twitchIdentityApiService.GetWorkerAuthorizationCodeUrl();
        }

        [HttpGet("oidc/url")]
        public string GetOIDCUrl()
        {
            return _twitchIdentityApiService.GetClientOIDCAuthorizationCodeUrl();
        }

        [HttpGet("token/{code}")]
        public async Task<ClientCredentialsTokenResponse> GetToken(string code)
        {
            return await _twitchIdentityApiService.GetToken(code);
        }
    }
}
