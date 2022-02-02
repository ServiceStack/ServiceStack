using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.Redis.Pipeline
{
    public partial class RedisPipelineCommand
    {
        private readonly RedisNativeClient client;
        private int cmdCount;

        public RedisPipelineCommand(RedisNativeClient client)
        {
            this.client = client;
        }

        public void WriteCommand(params byte[][] cmdWithBinaryArgs)
        {
            client.WriteAllToSendBuffer(cmdWithBinaryArgs);
            cmdCount++;
        }

        public List<long> ReadAllAsInts()
        {
            var results = new List<long>();
            while (cmdCount-- > 0)
            {
                results.Add(client.ReadLong());
            }

            return results;
        }

        public bool ReadAllAsIntsHaveSuccess()
        {
            var allResults = ReadAllAsInts();
            return allResults.All(x => x == RedisNativeClient.Success);
        }

        public void Flush()
        {
            client.FlushAndResetSendBuffer();
        }
    }
}