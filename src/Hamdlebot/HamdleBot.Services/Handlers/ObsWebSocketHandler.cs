using System.Text.Json;
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
    public ObsWebSocketHandler(ObsSettings obsSettings, CancellationToken cancellationToken, byte maxReconnectAttempts)
        : base(obsSettings.SocketUrl!, cancellationToken, maxReconnectAttempts)
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
        var message = JsonSerializer.Serialize(request);
        await SendMessage(message);
    }
    
    private async Task ProcessMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }
            
        var authResponse = JsonSerializer.Deserialize<ObsResponse<ObsAuthenticationResponse>>(message);

        if (authResponse == null)
        {
            return;
        }

        if (authResponse.OpCode is OpCodeType.Hello or OpCodeType.Reidentify)
        {
            await SendIdentifyRequest();
            return;
        }

        var sceneItemListResponse = JsonSerializer.Deserialize<ObsResponse<GetSceneItemListResponse>>(message);

        if (sceneItemListResponse == null)
        {
            return;
        }

        if (sceneItemListResponse.Response.RequestType.Equals("GetSceneItemList", StringComparison.CurrentCultureIgnoreCase))
        {
            GetHamdleScene(sceneItemListResponse.Response.ResponseData);
        }
    }
    
    private void SetUpEventHandlers()
    {
        MessageReceived += async (message) =>
        {
            await ProcessMessage(message);
        };
    }
    
    private async Task SendIdentifyRequest()
    {
        var req = new ObsRequest<IdentifyRequest>
        {
            RequestData = new RequestWrapper<IdentifyRequest>
            {
                RpcVersion = 1,
                Authentication = _obsSettings.ObsAuthentication!
            },
            Op = OpCodeType.Identify,
        };
        var json = JsonSerializer.Serialize(req);
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