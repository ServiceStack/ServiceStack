using ServiceStack.Text;
using System;

namespace ServiceStack.Redis
{
    public partial class RedisLock
        : IDisposable
    {
        private readonly object untypedClient;
        private readonly string key;

        private RedisLock(object redisClient, string key)
        {
            this.untypedClient = redisClient;
            this.key = key;
        }

        public RedisLock(IRedisClient redisClient, string key, TimeSpan? timeOut)
             : this(redisClient, key)
        {
            ExecUtils.RetryUntilTrue(
                () =>
                    {
                        //This pattern is taken from the redis command for SETNX http://redis.io/commands/setnx

                        //Calculate a unix time for when the lock should expire
                        var realSpan = timeOut ?? new TimeSpan(365, 0, 0, 0); //if nothing is passed in the timeout hold for a year
                        var expireTime = DateTime.UtcNow.Add(realSpan);
                        var lockString = (expireTime.ToUnixTimeMs() + 1).ToString();

                        //Try to set the lock, if it does not exist this will succeed and the lock is obtained
                        var nx = redisClient.SetValueIfNotExists(key, lockString);
                        if (nx)
                            return true;

                        //If we've gotten here then a key for the lock is present. This could be because the lock is
                        //correctly acquired or it could be because a client that had acquired the lock crashed (or didn't release it properly).
                        //Therefore we need to get the value of the lock to see when it should expire

                        redisClient.Watch(key);
                        var lockExpireString = redisClient.Get<string>(key);
                        if (!long.TryParse(lockExpireString, out var lockExpireTime))
                        {
                            redisClient.UnWatch();  // since the client is scoped externally
                            return false;
                        }

                        //If the expire time is greater than the current time then we can't let the lock go yet
                        if (lockExpireTime > DateTime.UtcNow.ToUnixTimeMs())
                        {
                            redisClient.UnWatch();  // since the client is scoped externally
                            return false;
                        }

                        //If the expire time is less than the current time then it wasn't released properly and we can attempt to 
                        //acquire the lock. The above call to Watch(_lockKey) enrolled the key in monitoring, so if it changes
                        //before we call Commit() below, the Commit will fail and return false, which means that another thread 
                        //was able to acquire the lock before we finished processing.
                        using (var trans = redisClient.CreateTransaction()) // we started the "Watch" above; this tx will succeed if the value has not moved 
                        {
                            trans.QueueCommand(r => r.Set(key, lockString));
                            return trans.Commit(); //returns false if Transaction failed
                        }
                    },
                timeOut
            );
        }

        public void Dispose()
        {
            ((IRedisClient)untypedClient).Remove(key);
        }
    }
}