using System.Collections.Generic;

namespace ServiceStack.Redis;

public interface IHasStats
{
    Dictionary<string, long> Stats { get; }
}