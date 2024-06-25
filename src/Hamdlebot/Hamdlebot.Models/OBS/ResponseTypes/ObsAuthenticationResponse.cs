namespace Hamdlebot.Models.OBS.ResponseTypes;

public class ObsAuthenticationResponse
{
    public string Challenge { get; set; }
    public string Salt { get; set; }
}