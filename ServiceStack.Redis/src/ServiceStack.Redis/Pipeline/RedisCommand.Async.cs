using ServiceStack.Redis.Internal;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceStack.Redis
{
    /// <summary>
    /// Redis command that does not get queued
    /// </summary>
    internal partial class RedisCommand
    {
        private Delegate _asyncReturnCommand;
        protected Delegate AsyncReturnCommand => _asyncReturnCommand;
        private RedisCommand SetAsyncReturnCommand(Delegate value)
        {
            if (_asyncReturnCommand is object && _asyncReturnCommand != value)
                throw new InvalidOperationException("Only a single async return command can be assigned");
            _asyncReturnCommand = value;
            return this;
        }
        internal RedisCommand WithAsyncReturnCommand(Func<IRedisClientAsync, ValueTask> VoidReturnCommandAsync)
            => SetAsyncReturnCommand(VoidReturnCommandAsync);
        internal RedisCommand WithAsyncReturnCommand(Func<IRedisClientAsync, ValueTask<int>> IntReturnCommandAsync)
            => SetAsyncReturnCommand(IntReturnCommandAsync);
        internal RedisCommand WithAsyncReturnCommand(Func<IRedisClientAsync, ValueTask<long>> LongReturnCommandAsync)
            => SetAsyncReturnCommand(LongReturnCommandAsync);
        internal RedisCommand WithAsyncReturnCommand(Func<IRedisClientAsync, ValueTask<bool>> BoolReturnCommandAsync)
            => SetAsyncReturnCommand(BoolReturnCommandAsync);
        internal RedisCommand WithAsyncReturnCommand(Func<IRedisClientAsync, ValueTask<byte[]>> BytesReturnCommandAsync)
            => SetAsyncReturnCommand(BytesReturnCommandAsync);
        internal RedisCommand WithAsyncReturnCommand(Func<IRedisClientAsync, ValueTask<byte[][]>> MultiBytesReturnCommandAsync)
            => SetAsyncReturnCommand(MultiBytesReturnCommandAsync);
        internal RedisCommand WithAsyncReturnCommand(Func<IRedisClientAsync, ValueTask<string>> StringReturnCommandAsync)
            => SetAsyncReturnCommand(StringReturnCommandAsync);
        internal RedisCommand WithAsyncReturnCommand(Func<IRedisClientAsync, ValueTask<List<string>>> MultiStringReturnCommandAsync)
            => SetAsyncReturnCommand(MultiStringReturnCommandAsync);
        internal RedisCommand WithAsyncReturnCommand(Func<IRedisClientAsync, ValueTask<Dictionary<string, string>>> DictionaryStringReturnCommandAsync)
            => SetAsyncReturnCommand(DictionaryStringReturnCommandAsync);
        internal RedisCommand WithAsyncReturnCommand(Func<IRedisClientAsync, ValueTask<RedisData>> RedisDataReturnCommandAsync)
            => SetAsyncReturnCommand(RedisDataReturnCommandAsync);
        internal RedisCommand WithAsyncReturnCommand(Func<IRedisClientAsync, ValueTask<RedisText>> RedisTextReturnCommandAsync)
            => SetAsyncReturnCommand(RedisTextReturnCommandAsync);
        internal RedisCommand WithAsyncReturnCommand(Func<IRedisClientAsync, ValueTask<double>> DoubleReturnCommandAsync)
            => SetAsyncReturnCommand(DoubleReturnCommandAsync);

        public override ValueTask ExecuteAsync(IRedisClientAsync client)
        {
            try
            {
                switch (_asyncReturnCommand)
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
                    case Func<IRedisClientAsync, ValueTask<Dictionary<string, string>>> DictionaryStringReturnCommandAsync:
                        return DictionaryStringReturnCommandAsync(client).Await();
                    case Func<IRedisClientAsync, ValueTask<RedisData>> RedisDataReturnCommandAsync:
                        return RedisDataReturnCommandAsync(client).Await();
                    case Func<IRedisClientAsync, ValueTask<RedisText>> RedisTextReturnCommandAsync:
                        return RedisTextReturnCommandAsync(client).Await();
                    case Func<IRedisClientAsync, ValueTask<bool>> BoolReturnCommandAsync:
                        return BoolReturnCommandAsync(client).Await();
                    case object obj:
                        ExecuteThrowIfSync();
                        return default;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return default; // RedisCommand.Execute swallows here; we'll do the same
            }
        }

        partial void OnExecuteThrowIfAsync()
        {
            if (_asyncReturnCommand is object)
            {
                throw new InvalidOperationException("An async return command was present, but the queued operation is being processed synchronously");
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
                || DictionaryStringReturnCommand is object
                || RedisDataReturnCommand is object
                || RedisTextReturnCommand is object
                || DoubleReturnCommand is object)
            {
                throw new InvalidOperationException("A sync return command was present, but the queued operation is being processed asynchronously");
            }
        }
    }
}