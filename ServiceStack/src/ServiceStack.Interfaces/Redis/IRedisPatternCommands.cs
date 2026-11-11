using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis;

/// <summary>Optional, standalone SCAN + UNLINK operations on an exclusively leased writer.</summary>
public interface IRedisPatternCommands
{
    long UnlinkByPattern(string pattern, int batchSize = 512, int scanCount = 1000, CancellationToken token = default);
}

public interface IRedisPatternCommandsAsync
{
    ValueTask<long> UnlinkByPatternAsync(string pattern, int batchSize = 512, int scanCount = 1000, CancellationToken token = default);
}
