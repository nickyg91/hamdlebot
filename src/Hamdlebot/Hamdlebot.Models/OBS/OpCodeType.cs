namespace Hamdlebot.Models.OBS;

public enum OpCodeType : byte
{
    Hello = 0,
    Identify = 1,
    Identified = 2,
    Reidentify = 3,
    Event = 4,
    Request = 5,
    RequestResponse = 6,
    RequestBatch = 7,
    RequestBatchResponse = 8
}