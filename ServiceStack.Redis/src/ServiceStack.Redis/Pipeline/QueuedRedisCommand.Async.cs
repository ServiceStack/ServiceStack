using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using ServiceStack.Redis.Internal;

namespace ServiceStack.Redis.Pipeline
{
    /// <summary>
    /// A complete redis command, with method to send command, receive response, and run callback on success or failure
    /// </summary>
    internal partial class QueuedRedisCommand : RedisCommand
    {
        public override ValueTask ExecuteAsync(IRedisClientAsync client)
        {
            try
            {
                switch (AsyncReturnCommand)
                {
                    case null:
                        ExecuteThrowIfSync();
                        return default;
                    case Func<IRedisClientAsync, ValueTask> VoidReturnCommandAsync:
                        return VoidReturnCommandAsync(client);
                    case Func<IRedisClientAsync, ValueTask<int>> IntReturnCommandAsync:
                        return IntReturnCommandAsync(client).Await();
                    case Func<IRedisClientAsync, ValueTask<long>> LongReturnCommandAsync:
                        return LongReturnCommandAsync(client).Await();
                    case Func<IRedisClientAsync, ValueTask<double>> DoubleReturnCommandAsync:
                        return DoubleReturnCommandAsync(client).Await();
                    case Func<IRedisClientAsync, ValueTask<byte[]>> BytesReturnCommandAsync:
                        return BytesReturnCommandAsync(client).Await();
                    case Func<IRedisClientAsync, ValueTask<string>> StringReturnCommandAsync:
                        return StringReturnCommandAsync(client).Await();
                    case Func<IRedisClientAsync, ValueTask<byte[][]>> MultiBytesReturnCommandAsync:
                        return MultiBytesReturnCommandAsync(client).Await();
                    case Func<IRedisClientAsync, ValueTask<List<string>>> MultiStringReturnCommandAsync:
                        return MultiStringReturnCommandAsync(client).Await();
                    case object obj:
                        ExecuteThrowIfSync();
                        // Execute only processes a limited number of patterns; we'll respect that here too
                        throw new InvalidOperationException("Command cannot be executed in this context: " + obj.GetType().FullName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                throw;
            }
        }
    }
}