using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.Redis
{
	public class RedisPipelineCommand 
	{
		private readonly RedisNativeClient client;
        private Queue<RedisNativeClient.ExpectIntCommand> commands = new Queue<RedisNativeClient.ExpectIntCommand>();

		public RedisPipelineCommand(RedisNativeClient client)
		{
			this.client = client;
		}

		public void WriteCommand(params byte[][] cmdWithBinaryArgs)
		{
            RedisNativeClient.ExpectIntCommand cmd = new RedisNativeClient.ExpectIntCommand(client);
            cmd.init(cmdWithBinaryArgs);
            cmd.execute();
            commands.Enqueue(cmd);
		}

		public List<int> ReadAllAsInts()
		{
			var results = new List<int>();
            if (commands.Count() == 0)
                return results;
            while (commands.Count() > 0)
            {
                results.Add(commands.Dequeue().getInt());
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
			client.FlushSendBuffer();
		}
	}
}
