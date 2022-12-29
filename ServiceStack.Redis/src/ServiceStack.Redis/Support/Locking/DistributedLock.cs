using System;
using System.Diagnostics;

namespace ServiceStack.Redis.Support.Locking
{
    public partial class DistributedLock : IDistributedLock
    {
        public const int LOCK_NOT_ACQUIRED = 0;
        public const int LOCK_ACQUIRED = 1;
        public const int LOCK_RECOVERED = 2;

        /// <summary>
        /// acquire distributed, non-reentrant lock on key
        /// </summary>
        /// <param name="key">global key for this lock</param>
        /// <param name="acquisitionTimeout">timeout for acquiring lock</param>
        /// <param name="lockTimeout">timeout for lock, in seconds (stored as value against lock key) </param>
        /// <param name="client"></param>
        /// <param name="lockExpire"></param>
        public virtual long Lock(string key, int acquisitionTimeout, int lockTimeout, out long lockExpire, IRedisClient client)
        {
            lockExpire = 0;

            // cannot lock on a null key
            if (key == null)
                return LOCK_NOT_ACQUIRED;

            const int sleepIfLockSet = 200;
            acquisitionTimeout *= 1000; //convert to ms
            int tryCount = (acquisitionTimeout / sleepIfLockSet) + 1;

            var ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            var newLockExpire = CalculateLockExpire(ts, lockTimeout);

            var localClient = (RedisClient)client;
            long wasSet = localClient.SetNX(key, BitConverter.GetBytes(newLockExpire));
            int totalTime = 0;
            while (wasSet == LOCK_NOT_ACQUIRED && totalTime < acquisitionTimeout)
            {
                int count = 0;
                while (wasSet == 0 && count < tryCount && totalTime < acquisitionTimeout)
                {
                    TaskUtils.Sleep(sleepIfLockSet);
                    totalTime += sleepIfLockSet;
                    ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
                    newLockExpire = CalculateLockExpire(ts, lockTimeout);
                    wasSet = localClient.SetNX(key, BitConverter.GetBytes(newLockExpire));
                    count++;
                }
                // acquired lock!
                if (wasSet != LOCK_NOT_ACQUIRED) break;

                // handle possibliity of crashed client still holding the lock
                using (var pipe = localClient.CreatePipeline())
                {
                    long lockValue = 0;
                    pipe.QueueCommand(r => ((RedisNativeClient)r).Watch(key));
                    pipe.QueueCommand(r => ((RedisNativeClient)r).Get(key), x => lockValue = (x != null) ? BitConverter.ToInt64(x, 0) : 0);
                    pipe.Flush();

                    // if lock value is 0 (key is empty), or expired, then we can try to acquire it
                    ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
                    if (lockValue < ts.TotalSeconds)
                    {
                        ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
                        newLockExpire = CalculateLockExpire(ts, lockTimeout);
                        using (var trans = localClient.CreateTransaction())
                        {
                            var expire = newLockExpire;
                            trans.QueueCommand(r => ((RedisNativeClient)r).Set(key, BitConverter.GetBytes(expire)));
                            if (trans.Commit())
                                wasSet = LOCK_RECOVERED; //recovered lock!
                        }
                    }
                    else
                    {
                        localClient.UnWatch();
                    }
                }
                if (wasSet != LOCK_NOT_ACQUIRED) break;
                TaskUtils.Sleep(sleepIfLockSet);
                totalTime += sleepIfLockSet;
            }
            if (wasSet != LOCK_NOT_ACQUIRED)
            {
                lockExpire = newLockExpire;
            }
            return wasSet;
        }

        /// <summary>
        /// unlock key
        /// </summary>
        public virtual bool Unlock(string key, long lockExpire, IRedisClient client)
        {
            if (lockExpire <= 0)
                return false;
            long lockVal = 0;
            var localClient = (RedisClient)client;
            using (var pipe = localClient.CreatePipeline())
            {

                pipe.QueueCommand(r => ((RedisNativeClient)r).Watch(key));
                pipe.QueueCommand(r => ((RedisNativeClient)r).Get(key),
                                  x => lockVal = (x != null) ? BitConverter.ToInt64(x, 0) : 0);
                pipe.Flush();
            }

            if (lockVal != lockExpire)
            {
                if (lockVal != 0)
                    Debug.WriteLine($"Unlock(): Failed to unlock key {key}; lock has been acquired by another client ");
                else
                    Debug.WriteLine($"Unlock(): Failed to unlock key {key}; lock has been identifed as a zombie and harvested ");
                localClient.UnWatch();
                return false;
            }

            using (var trans = localClient.CreateTransaction())
            {
                trans.QueueCommand(r => ((RedisNativeClient)r).Del(key));
                var rc = trans.Commit();
                if (!rc)
                    Debug.WriteLine($"Unlock(): Failed to delete key {key}; lock has been acquired by another client ");
                return rc;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        private static long CalculateLockExpire(TimeSpan ts, int timeout)
        {
            return (long)(ts.TotalSeconds + timeout + 1.5);
        }
    }
}