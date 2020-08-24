using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack
{
    public interface ISequenceSourceAsync : IRequiresSchema
    {
        Task<long> IncrementAsync(string key, long amount = 1, CancellationToken token = default);

        Task Reset(string key, long startingAt = 0, CancellationToken token = default);
    }
}