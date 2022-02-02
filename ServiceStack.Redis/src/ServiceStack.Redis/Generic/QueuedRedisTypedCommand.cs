using ServiceStack.Redis.Pipeline;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Generic
{
    /// <summary>
    /// A complete redis command, with method to send command, receive response, and run callback on success or failure
    /// </summary>
    internal partial class QueuedRedisTypedCommand<T> : QueuedRedisOperation
    {

        public Action<IRedisTypedClient<T>> VoidReturnCommand { get; set; }
        public Func<IRedisTypedClient<T>, int> IntReturnCommand { get; set; }
        public Func<IRedisTypedClient<T>, long> LongReturnCommand { get; set; }
        public Func<IRedisTypedClient<T>, bool> BoolReturnCommand { get; set; }
        public Func<IRedisTypedClient<T>, byte[]> BytesReturnCommand { get; set; }
        public Func<IRedisTypedClient<T>, byte[][]> MultiBytesReturnCommand { get; set; }
        public Func<IRedisTypedClient<T>, string> StringReturnCommand { get; set; }
        public Func<IRedisTypedClient<T>, List<string>> MultiStringReturnCommand { get; set; }
        public Func<IRedisTypedClient<T>, List<T>> MultiObjectReturnCommand { get; set; }
        public Func<IRedisTypedClient<T>, double> DoubleReturnCommand { get; set; }
        public Func<IRedisTypedClient<T>, T> ObjectReturnCommand { get; set; }

        public void Execute(IRedisTypedClient<T> client)
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
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        private void ExecuteThrowIfAsync() => OnExecuteThrowIfAsync();
        partial void OnExecuteThrowIfAsync();
    }
}
