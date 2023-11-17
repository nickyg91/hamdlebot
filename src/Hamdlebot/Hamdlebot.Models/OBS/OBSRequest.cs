namespace Hamdlebot.Models.OBS;

public class OBSRequest<T> where T : class 
{
    public string RequestType { get; set; }
    public string RequestId { get; set; }
    public T RequestData { get; set; }
}