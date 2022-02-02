using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Support.Locking
{
    partial class DistributedLock : IDistributedLockAsync
    {
        public IDistributedLockAsync AsAsync() => this;

        async ValueTask<LockState> IDistributedLockAsync.LockAsync(string key, int acquisitionTimeout, int lockTimeout, IRedisClientAsync client, CancellationToken token)
        {
            long lockExpire = 0;

            // cannot lock on a null key
            if (key == null)
                return new LockState(LOCK_NOT_ACQUIRED, lockExpire);

            const int sleepIfLockSet = 200;
            acquisitionTimeout *= 1000; //convert to ms
            int tryCount = (acquisitionTimeout / sleepIfLockSet) + 1;

            var ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            var newLockExpire = CalculateLockExpire(ts, lockTimeout);

            var nativeClient = (IRedisNativeClientAsync)client;
            long wasSet = await nativeClient.SetNXAsync(key, BitConverter.GetBytes(newLockExpire), token).ConfigureAwait(false);
            int totalTime = 0;
            while (wasSet == LOCK_NOT_ACQUIRED && totalTime < acquisitionTimeout)
            {
                int count = 0;
                while (wasSet == 0 && count < tryCount && totalTime < acquisitionTimeout)
                {
                    await Task.Delay(sleepIfLockSet).ConfigureAwait(false);
                    totalTime += sleepIfLockSet;
                    ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
                    newLockExpire = CalculateLockExpire(ts, lockTimeout);
                    wasSet = await nativeClient.SetNXAsync(key, BitConverter.GetBytes(newLockExpire), token).ConfigureAwait(false);
                    count++;
                }
                // acquired lock!
                if (wasSet != LOCK_NOT_ACQUIRED) break;

                // handle possibliity of crashed client still holding the lock
                var pipe = client.CreatePipeline();
                await using (pipe.ConfigureAwait(false))
                {
                    long lockValue = 0;
                    pipe.QueueCommand(r => ((IRedisNativeClientAsync)r).WatchAsync(new[] { key }, token));
                    pipe.QueueCommand(r => ((IRedisNativeClientAsync)r).GetAsync(key, token), x => lockValue = (x != null) ? BitConverter.ToInt64(x, 0) : 0);
                    await pipe.FlushAsync(token).ConfigureAwait(false);

                    // if lock value is 0 (key is empty), or expired, then we can try to acquire it
                    ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
                    if (lockValue < ts.TotalSeconds)
                    {
                        ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
                        newLockExpire = CalculateLockExpire(ts, lockTimeout);
                        var trans = await client.CreateTransactionAsync(token).ConfigureAwait(false);
                        await using (trans.ConfigureAwait(false))
                        {
                            var expire = newLockExpire;
                            trans.QueueCommand(r => ((IRedisNativeClientAsync)r).SetAsync(key, BitConverter.GetBytes(expire), token: token));
                            if (await trans.CommitAsync(token).ConfigureAwait(false))
                                wasSet = LOCK_RECOVERED; //recovered lock!
                        }
                    }
                    else
                    {
                        await nativeClient.UnWatchAsync(token).ConfigureAwait(false);
                    }
                }
                if (wasSet != LOCK_NOT_ACQUIRED) break;
                await Task.Delay(sleepIfLockSet).ConfigureAwait(false);
                totalTime += sleepIfLockSet;
            }
            if (wasSet != LOCK_NOT_ACQUIRED)
            {
                lockExpire = newLockExpire;
            }
            return new LockState(wasSet, lockExpire);
        }

        async ValueTask<bool> IDistributedLockAsync.UnlockAsync(string key, long lockExpire, IRedisClientAsync client, CancellationToken token)
        {
            if (lockExpire <= 0)
                return false;
            long lockVal = 0;
            var nativeClient = (IRedisNativeClientAsync)client;
            var pipe = client.CreatePipeline();
            await using (pipe.ConfigureAwait(false))
            {
                pipe.QueueCommand(r => ((IRedisNativeClientAsync)r).WatchAsync(new[] { key }, token));
                pipe.QueueCommand(r => ((IRedisNativeClientAsync)r).GetAsync(key, token),
                                  x => lockVal = (x != null) ? BitConverter.ToInt64(x, 0) : 0);
                await pipe.FlushAsync(token).ConfigureAwait(false);
            }

            if (lockVal != lockExpire)
            {
                if (lockVal != 0)
                    Debug.WriteLine($"Unlock(): Failed to unlock key {key}; lock has been acquired by another client ");
                else
                    Debug.WriteLine($"Unlock(): Failed to unlock key {key}; lock has been identifed as a zombie and harvested ");
                await nativeClient.UnWatchAsync(token).ConfigureAwait(false);
                return false;
            }

            var trans = await client.CreateTransactionAsync(token).ConfigureAwait(false);
            await using (trans.ConfigureAwait(false))
            {
                trans.QueueCommand(r => ((IRedisNativeClientAsync)r).DelAsync(key, token));
                var rc = await trans.CommitAsync(token).ConfigureAwait(false);
                if (!rc)
                    Debug.WriteLine($"Unlock(): Failed to delete key {key}; lock has been acquired by another client ");
                return rc;
            }
        }
    }
}