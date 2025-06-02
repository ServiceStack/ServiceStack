#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack;

public interface ISequenceSourceAsync : IRequiresSchema
{
    Task<long> IncrementAsync(string key, long amount = 1, CancellationToken token = default);

    Task ResetAsync(string key, long startingAt = 0, CancellationToken token = default);
}