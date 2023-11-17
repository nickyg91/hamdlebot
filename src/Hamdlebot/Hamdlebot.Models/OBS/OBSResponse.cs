namespace Hamdlebot.Models.OBS;

public class OBSResponse<T> where T : class
{
    public OpCodeType Op { get; set; }
    public T D { get; set; }
}