using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ServiceStack.Redis.Generic;
using ServiceStack.Redis.Internal;
using ServiceStack.Redis.Pipeline;

namespace ServiceStack.Redis.Generic
{
    /// <summary>
    /// A complete redis command, with method to send command, receive response, and run callback on success or failure
    /// </summary>
    internal partial class QueuedRedisTypedCommand<T> : QueuedRedisOperation
    {
        private Delegate _asyncReturnCommand;
        partial void OnExecuteThrowIfAsync()
        {
            if (_asyncReturnCommand is object)
            {
                throw new InvalidOperationException("An async return command was present, but the queued operation is being processed synchronously");
            }
        }
        private QueuedRedisTypedCommand<T> SetAsyncReturnCommand(Delegate value)
        {
            if (_asyncReturnCommand is object && _asyncReturnCommand != value)
                throw new InvalidOperationException("Only a single async return command can be assigned");
            _asyncReturnCommand = value;
            return this;
        }

        internal QueuedRedisTypedCommand<T> WithAsyncReturnCommand(Func<IRedisTypedClientAsync<T>, ValueTask> VoidReturnCommandAsync)
           => SetAsyncReturnCommand(VoidReturnCommandAsync);
        internal QueuedRedisTypedCommand<T> WithAsyncReturnCommand(Func<IRedisTypedClientAsync<T>, ValueTask<int>> IntReturnCommandAsync)
            => SetAsyncReturnCommand(IntReturnCommandAsync);
        internal QueuedRedisTypedCommand<T> WithAsyncReturnCommand(Func<IRedisTypedClientAsync<T>, ValueTask<long>> LongReturnCommandAsync)
            => SetAsyncReturnCommand(LongReturnCommandAsync);
        internal QueuedRedisTypedCommand<T> WithAsyncReturnCommand(Func<IRedisTypedClientAsync<T>, ValueTask<bool>> BoolReturnCommandAsync)
            => SetAsyncReturnCommand(BoolReturnCommandAsync);
        internal QueuedRedisTypedCommand<T> WithAsyncReturnCommand(Func<IRedisTypedClientAsync<T>, ValueTask<byte[]>> BytesReturnCommandAsync)
            => SetAsyncReturnCommand(BytesReturnCommandAsync);
        internal QueuedRedisTypedCommand<T> WithAsyncReturnCommand(Func<IRedisTypedClientAsync<T>, ValueTask<byte[][]>> MultiBytesReturnCommandAsync)
            => SetAsyncReturnCommand(MultiBytesReturnCommandAsync);
        internal QueuedRedisTypedCommand<T> WithAsyncReturnCommand(Func<IRedisTypedClientAsync<T>, ValueTask<string>> StringReturnCommandAsync)
            => SetAsyncReturnCommand(StringReturnCommandAsync);
        internal QueuedRedisTypedCommand<T> WithAsyncReturnCommand(Func<IRedisTypedClientAsync<T>, ValueTask<List<string>>> MultiStringReturnCommandAsync)
            => SetAsyncReturnCommand(MultiStringReturnCommandAsync);
        internal QueuedRedisTypedCommand<T> WithAsyncReturnCommand(Func<IRedisTypedClientAsync<T>, ValueTask<double>> DoubleReturnCommandAsync)
            => SetAsyncReturnCommand(DoubleReturnCommandAsync);
        internal QueuedRedisTypedCommand<T> WithAsyncReturnCommand(Func<IRedisTypedClientAsync<T>, ValueTask<List<T>>> MultiObjectReturnCommandAsync)
            => SetAsyncReturnCommand(MultiObjectReturnCommandAsync);
        internal QueuedRedisTypedCommand<T> WithAsyncReturnCommand(Func<IRedisTypedClientAsync<T>, ValueTask<T>> ObjectReturnCommandAsync)
            => SetAsyncReturnCommand(ObjectReturnCommandAsync);

        public ValueTask ExecuteAsync(IRedisTypedClientAsync<T> client)
        {
            try
            {
                switch (_asyncReturnCommand)
                {
                    case null:
                        ExecuteThrowIfSync();
                        return default;
                    case Func<IRedisTypedClientAsync<T>, ValueTask> VoidReturnCommandAsync:
                        return VoidReturnCommandAsync(client);
                    case Func<IRedisTypedClientAsync<T>, ValueTask<int>> IntReturnCommandAsync:
                        return IntReturnCommandAsync(client).Await();
                    case Func<IRedisTypedClientAsync<T>, ValueTask<long>> LongReturnCommandAsync:
                        return LongReturnCommandAsync(client).Await();
                    case Func<IRedisTypedClientAsync<T>, ValueTask<double>> DoubleReturnCommandAsync:
                        return DoubleReturnCommandAsync(client).Await();
                    case Func<IRedisTypedClientAsync<T>, ValueTask<byte[]>> BytesReturnCommandAsync:
                        return BytesReturnCommandAsync(client).Await();
                    case Func<IRedisTypedClientAsync<T>, ValueTask<string>> StringReturnCommandAsync:
                        return StringReturnCommandAsync(client).Await();
                    case Func<IRedisTypedClientAsync<T>, ValueTask<byte[][]>> MultiBytesReturnCommandAsync:
                        return MultiBytesReturnCommandAsync(client).Await();
                    case Func<IRedisTypedClientAsync<T>, ValueTask<List<string>>> MultiStringReturnCommandAsync:
                        return MultiStringReturnCommandAsync(client).Await();
                    case object obj:
                        ExecuteThrowIfSync();
                        return default;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return default; // non-async version swallows
            }
        }

        protected void ExecuteThrowIfSync()
        {
            if (VoidReturnCommand is object
                || IntReturnCommand is object
                || LongReturnCommand is object
                || BoolReturnCommand is object
                || BytesReturnCommand is object
                || MultiBytesReturnCommand is object
                || StringReturnCommand is object
                || MultiStringReturnCommand is object
                || DoubleReturnCommand is object
                || MultiObjectReturnCommand is object
                || ObjectReturnCommand is object)
            {
                throw new InvalidOperationException("A sync return command was present, but the queued operation is being processed asynchronously");
            }
        }

    }
}
