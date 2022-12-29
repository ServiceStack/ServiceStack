using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Support.Locking
{
    /// <summary>
    /// Distributed lock interface
    /// </summary>
	public interface IDistributedLockAsync
    {
        // note: can't use "out" with async, so return LockState instead
        ValueTask<LockState> LockAsync(string key, int acquisitionTimeout, int lockTimeout, IRedisClientAsync client, CancellationToken token = default);
        ValueTask<bool> UnlockAsync(string key, long lockExpire, IRedisClientAsync client, CancellationToken token = default);
    }

    public readonly struct LockState
    {
        public long Result { get; } // kinda feels like this should be an enum; leaving alone for API parity (sync vs async)
        public long Expiration { get; }
        public LockState(long result, long expiration)
        {
            Result = result;
            Expiration = expiration;
        }
        public override bool Equals(object obj) => throw new NotSupportedException();
        public override int GetHashCode() => throw new NotSupportedException();
        public override string ToString() => nameof(LockState);

        public void Deconstruct(out long result, out long expiration)
        {
            result = Result;
            expiration = Expiration;
        }
    }
}