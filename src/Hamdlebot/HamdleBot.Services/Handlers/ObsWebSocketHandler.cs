using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hamdlebot.Core;
using Hamdlebot.Models.OBS;
using Hamdlebot.Models.OBS.RequestTypes;
using Hamdlebot.Models.OBS.ResponseTypes;
using OpCodeType = Hamdlebot.Models.OBS.OpCodeType;

namespace HamdleBot.Services.Handlers;

public class ObsWebSocketHandler : WebSocketHandlerBase
{
    private readonly ObsSettings _obsSettings;
    private int _hamdleSceneId;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    public override string Url => _obsSettings.SocketUrl!;
    public ObsWebSocketHandler(ObsSettings obsSettings, CancellationToken cancellationToken, byte maxReconnectAttempts)
        : base(cancellationToken, maxReconnectAttempts)
    {
        _obsSettings = obsSettings;
        SetUpEventHandlers();
    }
    
    public async Task SetHamdleSceneState(bool isEnabled)
    {
        var request = new ObsRequest<SetSceneItemEnabledRequest>
        {
            Op = OpCodeType.Request,
            RequestData = new RequestWrapper<SetSceneItemEnabledRequest>
            {
                RequestId = Guid.NewGuid(),
                RequestType = ObsRequestStrings.SetSceneItemEnabled,
                RequestData = new SetSceneItemEnabledRequest
                {
                    SceneName = _obsSettings.SceneName!,
                    SceneItemEnabled = isEnabled,
                    SceneItemId = _hamdleSceneId
                }
            }
        };
        var message = JsonSerializer.Serialize(request, _jsonSerializerOptions);
        await SendMessage(message);
    }
    
    private async Task GetSceneItemList()
    {
        var request = new ObsRequest<GetSceneItemListRequest>
        {
            Op = OpCodeType.Request,
            RequestData = new RequestWrapper<GetSceneItemListRequest>
            {
                RequestId = Guid.NewGuid(),
                RequestType = ObsRequestStrings.GetSceneItemList,
                RequestData = new GetSceneItemListRequest
                {
                    SceneName = _obsSettings.SceneName!
                }
            }
        };
        var message = JsonSerializer.Serialize(request, _jsonSerializerOptions);
        await SendMessage(message);
    }
    
    private async Task ProcessMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }
            
        var response = JsonSerializer.Deserialize<ObsResponse<ObsAuthenticationResponse>>(message, _jsonSerializerOptions);

        if (response is null)
        {
            return;
        }
        
        if (response.OpCode is OpCodeType.RequestResponse)
        {
            var sceneItemListResponse = JsonSerializer.Deserialize<ObsResponse<GetSceneItemListResponse>>(message, _jsonSerializerOptions);

            if (sceneItemListResponse == null)
            {
                return;
            }

            if (sceneItemListResponse.Response.RequestType.Equals("GetSceneItemList", StringComparison.CurrentCultureIgnoreCase))
            {
                GetHamdleScene(sceneItemListResponse.Response.ResponseData);
            }
        }
        else
        {
            switch (response.OpCode)
            {
                case OpCodeType.Identified:
                    await GetSceneItemList();
                    return;
                case OpCodeType.Hello or OpCodeType.Reidentify:
                    await SendIdentifyRequest(response.Response.Authentication);
                    return;
            }
        }
    }
    
    private void SetUpEventHandlers()
    {
        MessageReceived += async (message) =>
        {
            await ProcessMessage(message);
        };
    }
    
    private async Task SendIdentifyRequest(ObsAuthenticationResponse authResponse)
    {
        var ps = _obsSettings.ObsAuthentication! + authResponse.Salt;
        var binaryHash = SHA256.HashData(Encoding.UTF8.GetBytes(ps));
        var b64Hash = Convert.ToBase64String(binaryHash);
        var challengeHash = SHA256.HashData(Encoding.UTF8.GetBytes(b64Hash + authResponse.Challenge));
        var b64Challenge = Convert.ToBase64String(challengeHash);
        var req = new ObsRequest<IdentifyRequest>
        {
            RequestData = new RequestWrapper<IdentifyRequest>
            {
                RpcVersion = 1,
                Authentication = b64Challenge
            },
            Op = OpCodeType.Identify,
        };
        var json = JsonSerializer.Serialize(req, _jsonSerializerOptions);
        await SendMessage(json);
    }

    private void GetHamdleScene(GetSceneItemListResponse scenes)
    {
        var scene = scenes.SceneItems.FirstOrDefault(x => x.SourceName.ToLower().Equals(_obsSettings.HamdleSourceName, StringComparison.CurrentCultureIgnoreCase));
        if (scene == null)
        {
            return;
        }

        _hamdleSceneId = scene.SceneItemId;
    }
}