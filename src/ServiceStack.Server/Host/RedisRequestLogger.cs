using System;
using System.Collections.Generic;
using ServiceStack.Redis;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public class RedisRequestLogger : InMemoryRollingRequestLogger
    {
        private const string SortedSetKey = "log:requests";

        private readonly IRedisClientsManager redisManager;
        private int? loggerCapacity;

        public RedisRequestLogger(IRedisClientsManager redisManager, int? capacity = null)
        {
            this.redisManager = redisManager;
            this.loggerCapacity = capacity;
        }

        public override void Log(IRequest request, object requestDto, object response, TimeSpan requestDuration)
        {
            var requestType = requestDto != null ? requestDto.GetType() : null;

            if (ExcludeRequestType(requestType))
                return;

            using (var redis = redisManager.GetClient())
            {
                var redisLogEntry = redis.As<RequestLogEntry>();

                var entry = CreateEntry(request, requestDto, response, requestDuration, requestType);
                entry.Id = redisLogEntry.GetNextSequence();

                var key = UrnId.Create<RequestLogEntry>(entry.Id).ToLower();
                var nowScore = DateTime.UtcNow.ToUnixTime();

                using (var trans = redis.CreateTransaction())
                {
                    trans.QueueCommand(r => r.AddItemToSortedSet(SortedSetKey, key, nowScore));
                    trans.QueueCommand(r => r.Store(entry));

                    if (loggerCapacity != null)
                    {
                        trans.QueueCommand(r => r.RemoveRangeFromSortedSet(SortedSetKey, 0, -loggerCapacity.Value - 1));
                    }

                    trans.Commit();
                }
            }
        }

        public override List<RequestLogEntry> GetLatestLogs(int? take)
        {
            using (var redis = redisManager.GetClient())
            {
                var toRank = (int)(take.HasValue ? take - 1 : -1);
                var keys = redis.GetRangeFromSortedSetDesc(SortedSetKey, 0, toRank);
                var values = redis.As<RequestLogEntry>().GetValues(keys);
                return values;
            }
        }
    }
}