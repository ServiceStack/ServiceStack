using System;

namespace ServiceStack.Redis;

public class SlowlogItem
{
    public SlowlogItem(int id, DateTime timeStamp, int duration, string[] arguments)
    {
        Id = id;
        Timestamp = timeStamp;
        Duration = duration;
        Arguments = arguments;
    }

    public int Id { get; private set; }
    public int Duration { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string[] Arguments { get; private set; }
}