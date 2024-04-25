using System.Security.Cryptography;
using Hamdlebot.Core;
using Hamdlebot.Models;
using HamdleBot.Services.Twitch.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Hamdlebot.Web.Controllers
{
    [Route("api/twitch/auth")]
    [ApiController]
    public class TwitchAuthController : ControllerBase
    {
        private readonly IOptions<AppConfigSettings> _appConfigSettings;
        private readonly ITwitchIdentityApiService _twitchIdentityApiService;

        public TwitchAuthController(
            IOptions<AppConfigSettings> appConfigSettings,
            ITwitchIdentityApiService twitchIdentityApiService)
        {
            _appConfigSettings = appConfigSettings;
            _twitchIdentityApiService = twitchIdentityApiService;
        }

        [HttpGet]
        public string Get()
        {
            return
                $"https://id.twitch.tv/oauth2/authorize?response_type=code&client_id={_appConfigSettings.Value.TwitchConnectionInfo.ClientId}&redirect_uri=http://localhost:3000&scope=chat%3Aread+chat%3Aedit";
        }

        [HttpGet("oidc/url")]
        public string GetOIDCUrl()
        {
            var byteArray = new byte[20];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(byteArray);
            }

            var nonce = Convert.ToBase64String(byteArray);
            var url =
                $"https://id.twitch.tv/oauth2/authorize?response_type=code&client_id={_appConfigSettings.Value.TwitchConnectionInfo.ClientId}&redirect_uri=https://localhost:5002/authenticate&scope=channel%3Amanage%3Apolls+channel%3Aread%3Apolls+openid+user%3Aread%3Aemail&claims={{\"id_token\":{{\"email\":null,\"email_verified\":null}},\"userinfo\":{{\"email\":null,\"email_verified\":null,\"picture\":null,\"updated_at\":null}}}}&state={nonce}&nonce={nonce}";
            return url;
        }

        [HttpGet("token/{code}")]
        public async Task<ClientCredentialsTokenResponse> GetToken(string code)
        {
            var token = await _twitchIdentityApiService.GetToken(code);
            return token;
        }

        [Authorize("Twitch")]
        [HttpGet("test")]
        public string Test()
        {
            return "Test";
        }
    }
}
