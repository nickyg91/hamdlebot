using System.Text.Json;
using Hamdle.Cache;
using Hamdlebot.Core.Exceptions;
using Hamdlebot.Models;
using HamdleBot.Services.Twitch.Interfaces;
using Hamdlebot.Web.Security.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace Hamdlebot.Web.Controllers
{
    [Route("api/twitch/auth")]
    [ApiController]
    public class TwitchAuthController : ControllerBase
    {
        private readonly ITwitchIdentityApiService _twitchIdentityApiService;
        private readonly ICacheService _cacheService;
        public TwitchAuthController(
            ITwitchIdentityApiService twitchIdentityApiService, ICacheService cacheService)
        {
            _twitchIdentityApiService = twitchIdentityApiService;
            _cacheService = cacheService;
        }

        [HttpGet, AuthorizedTwitchUser]
        public string Get()
        {
            return _twitchIdentityApiService.GetWorkerAuthorizationCodeUrl();
        }

        [HttpGet("oidc/url")]
        public string GetOidcUrl()
        {
            return _twitchIdentityApiService.GetClientOidcAuthorizationCodeUrl();
        }

        [HttpGet("token/bot/{code}")]
        public async Task<ClientCredentialsTokenResponse> GetBotToken(string code)
        {
            var token = await _twitchIdentityApiService.GetTokenForBot(code);
            if (token == null)
            {
                throw new TokenGenerationException("An error occurred while generating a twitch token.");
            }
            await _cacheService.AddItem(CacheKeyType.TwitchOauthToken, token!.AccessToken,
                TimeSpan.FromSeconds(token.ExpiresIn));
            await _cacheService.AddItem(CacheKeyType.TwitchRefreshToken, token.RefreshToken, TimeSpan.FromDays(30));
            await _cacheService
                .Subscriber
                .PublishAsync(
                    new RedisChannel(RedisChannelType.BotTwitchToken, RedisChannel.PatternMode.Auto),
                    JsonSerializer.Serialize(token));
            return token;
        }
        
        [HttpGet("token/{code}")]
        public async Task<ClientCredentialsTokenResponse> GetToken(string code)
        {
            return await _twitchIdentityApiService.GetToken(code);
        }
    }
}
