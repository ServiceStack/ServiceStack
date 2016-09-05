using System;
using ServiceStack.Redis;

namespace ServiceStack.Validation
{
    public class ExecOnceOnly : IDisposable
    {
        private const string Flag = "Y";

        private readonly string hashKey;

        private readonly string correlationId;

        private readonly IRedisClient redis;

        public ExecOnceOnly(IRedisClientsManager redisManager, Type forType, string correlationId)
            : this(redisManager, "hash:nx:" + forType.GetOperationName(), correlationId) { }

        public ExecOnceOnly(IRedisClientsManager redisManager, Type forType, Guid? correlationId)
            : this(redisManager, "hash:nx:" + forType.GetOperationName(), (correlationId.HasValue ? correlationId.Value.ToString("N") : null)) { }

        public ExecOnceOnly(IRedisClientsManager redisManager, string hashKey, string correlationId)
        {
            redisManager.ThrowIfNull("redisManager");
            hashKey.ThrowIfNull("hashKey");

            this.hashKey = hashKey;
            this.correlationId = correlationId;

            if (correlationId != null)
            {
                redis = redisManager.GetClient();
                var exists = !redis.SetEntryInHashIfNotExists(hashKey, correlationId, Flag);
                if (exists)
                    throw HttpError.Conflict(ErrorMessages.RequestAlreadyProcessedFmt.Fmt(correlationId));
            }
        }

        public bool Executed { get; private set; }

        public void Commit()
        {
            this.Executed = true;
        }

        public void Rollback()
        {
            if (redis == null) return;

            redis.RemoveEntryFromHash(hashKey, correlationId);
            this.Executed = false;
        }

        public void Dispose()
        {
            if (correlationId != null && !Executed)
            {
                Rollback();
            }
            redis?.Dispose(); //release back into the pool.
        }
    }
}