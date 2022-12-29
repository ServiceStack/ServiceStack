using System;
using System.Diagnostics;

namespace ServiceStack.Redis.Pipeline
{
    /// <summary>
    /// A complete redis command, with method to send command, receive response, and run callback on success or failure
    /// </summary>
    internal partial class QueuedRedisCommand : RedisCommand
    {
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
                else
                {
                    ExecuteThrowIfAsync();
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