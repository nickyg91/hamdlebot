namespace Hamdlebot.Models.OBS.RequestTypes;

public class SetSceneItemEnabledRequest
{
    public string SceneName { get; set; }
    public int SceneItemId { get; set; }
    public bool SceneItemEnabled { get; set; }
}