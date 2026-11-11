using System.Collections.Generic;

namespace ServiceStack.Redis;

public class RedisData
{
    /// <summary>True for RESP nil, distinguishing a nil array from an empty array without changing Children.</summary>
    public bool IsNull { get; set; }

    public byte[] Data { get; set; }

    public List<RedisData> Children { get; set; } 
}