using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Redis.Pipeline;

namespace ServiceStack.Redis
{
    /// <summary>
    /// Redis command that does not get queued
    /// </summary>
    internal partial class RedisCommand : QueuedRedisOperation
    {
        public Action<IRedisClient> VoidReturnCommand { get; set; }
        public Func<IRedisClient, int> IntReturnCommand { get; set; }
        public Func<IRedisClient, long> LongReturnCommand { get; set; }
        public Func<IRedisClient, bool> BoolReturnCommand { get; set; }
        public Func<IRedisClient, byte[]> BytesReturnCommand { get; set; }
        public Func<IRedisClient, byte[][]> MultiBytesReturnCommand { get; set; }
        public Func<IRedisClient, string> StringReturnCommand { get; set; }
        public Func<IRedisClient, List<string>> MultiStringReturnCommand { get; set; }
        public Func<IRedisClient, Dictionary<string, string>> DictionaryStringReturnCommand { get; set; }
        public Func<IRedisClient, RedisData> RedisDataReturnCommand { get; set; }
        public Func<IRedisClient, RedisText> RedisTextReturnCommand { get; set; }
        public Func<IRedisClient, double> DoubleReturnCommand { get; set; }

        public override void Execute(IRedisClient client)
        {
            try
            {
                if (VoidReturnCommand != null)
                {
                    VoidReturnCommand(client);

                }
                else if (IntReturnCommand != null)
                {
                    IntReturnCommand(client);

                }
                else if (LongReturnCommand != null)
                {
                    LongReturnCommand(client);

                }
                else if (DoubleReturnCommand != null)
                {
                    DoubleReturnCommand(client);

                }
                else if (BytesReturnCommand != null)
                {
                    BytesReturnCommand(client);

                }
                else if (StringReturnCommand != null)
                {
                    StringReturnCommand(client);

                }
                else if (MultiBytesReturnCommand != null)
                {
                    MultiBytesReturnCommand(client);

                }
                else if (MultiStringReturnCommand != null)
                {
                    MultiStringReturnCommand(client);
                }
                else if (DictionaryStringReturnCommand != null)
                {
                    DictionaryStringReturnCommand(client);
                }
                else if (RedisDataReturnCommand != null)
                {
                    RedisDataReturnCommand(client);
                }
                else if (RedisTextReturnCommand != null)
                {
                    RedisTextReturnCommand(client);
                }
                else if (BoolReturnCommand != null)
                {
                    BoolReturnCommand(client);
                }
                else
                {
                    ExecuteThrowIfAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        protected void ExecuteThrowIfAsync() => OnExecuteThrowIfAsync();
        partial void OnExecuteThrowIfAsync();
    }
}
