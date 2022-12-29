using ServiceStack.Redis.Internal;
using ServiceStack.Redis.Pipeline;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis
{
    partial class RedisNativeClient
        : IRedisNativeClientAsync
    {
        internal IRedisPipelineSharedAsync PipelineAsync
            => (IRedisPipelineSharedAsync)pipeline;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void AssertNotNull(object obj, string name = "key")
        {
            if (obj is null) Throw(name);
            static void Throw(string name) => throw new ArgumentNullException(name);
        }

        private IRedisNativeClientAsync AsAsync() => this;

        ValueTask IAsyncDisposable.DisposeAsync()
        {
            Dispose();
            return default;
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.TimeAsync(CancellationToken token)
            => SendExpectMultiDataAsync(token, Commands.Time);

        ValueTask<long> IRedisNativeClientAsync.ExistsAsync(string key, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectLongAsync(token, Commands.Exists, key.ToUtf8Bytes());
        }

        ValueTask<bool> IRedisNativeClientAsync.SetAsync(string key, byte[] value, bool exists, long expirySeconds, long expiryMilliseconds, CancellationToken token)
        {
            AssertNotNull(key);
            value ??= TypeConstants.EmptyByteArray;

            if (value.Length > OneGb)
                throw new ArgumentException("value exceeds 1G", nameof(value));

            var entryExists = exists ? Commands.Xx : Commands.Nx;
            byte[][] args;
            if (expiryMilliseconds != 0)
            {
                args = new[] { Commands.Set, key.ToUtf8Bytes(), value, Commands.Px, expiryMilliseconds.ToUtf8Bytes(), entryExists };
            }
            else if (expirySeconds != 0)
            {
                args = new[] { Commands.Set, key.ToUtf8Bytes(), value, Commands.Ex, expirySeconds.ToUtf8Bytes(), entryExists };
            }
            else
            {
                args = new[] { Commands.Set, key.ToUtf8Bytes(), value, entryExists };
            }

            return IsString(SendExpectStringAsync(token, args), OK);
        }
        ValueTask IRedisNativeClientAsync.SetAsync(string key, byte[] value, long expirySeconds, long expiryMilliseconds, CancellationToken token)
        {
            AssertNotNull(key);
            value ??= TypeConstants.EmptyByteArray;

            if (value.Length > OneGb)
                throw new ArgumentException("value exceeds 1G", nameof(value));

            byte[][] args;
            if (expiryMilliseconds != 0)
            {
                args = new[] { Commands.Set, key.ToUtf8Bytes(), value, Commands.Px, expiryMilliseconds.ToUtf8Bytes() };
            }
            else if (expirySeconds != 0)
            {
                args = new[] { Commands.Set, key.ToUtf8Bytes(), value, Commands.Ex, expirySeconds.ToUtf8Bytes() };
            }
            else
            {
                args = new[] { Commands.Set, key.ToUtf8Bytes(), value };
            }

            return SendExpectSuccessAsync(token, args);
        }

        ValueTask<byte[]> IRedisNativeClientAsync.GetAsync(string key, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectDataAsync(token, Commands.Get, key.ToUtf8Bytes());
        }

        ValueTask<long> IRedisNativeClientAsync.DelAsync(string key, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectLongAsync(token, Commands.Del, key.ToUtf8Bytes());
        }

        ValueTask<ScanResult> IRedisNativeClientAsync.ScanAsync(ulong cursor, int count, string match, CancellationToken token)
        {
            if (match == null)
                return SendExpectScanResultAsync(token, Commands.Scan, cursor.ToUtf8Bytes(),
                                            Commands.Count, count.ToUtf8Bytes());

            return SendExpectScanResultAsync(token, Commands.Scan, cursor.ToUtf8Bytes(),
                                        Commands.Match, match.ToUtf8Bytes(),
                                        Commands.Count, count.ToUtf8Bytes());
        }

        ValueTask<string> IRedisNativeClientAsync.TypeAsync(string key, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectCodeAsync(token, Commands.Type, key.ToUtf8Bytes());
        }

        ValueTask<long> IRedisNativeClientAsync.RPushAsync(string listId, byte[] value, CancellationToken token)
        {
            AssertListIdAndValue(listId, value);

            return SendExpectLongAsync(token, Commands.RPush, listId.ToUtf8Bytes(), value);
        }

        ValueTask<long> IRedisNativeClientAsync.SAddAsync(string setId, byte[] value, CancellationToken token)
        {
            AssertSetIdAndValue(setId, value);

            return SendExpectLongAsync(token, Commands.SAdd, setId.ToUtf8Bytes(), value);
        }

        ValueTask<long> IRedisNativeClientAsync.ZAddAsync(string setId, double score, byte[] value, CancellationToken token)
        {
            AssertSetIdAndValue(setId, value);

            return SendExpectLongAsync(token, Commands.ZAdd, setId.ToUtf8Bytes(), score.ToFastUtf8Bytes(), value);
        }

        ValueTask<long> IRedisNativeClientAsync.ZAddAsync(string setId, long score, byte[] value, CancellationToken token)
        {
            AssertSetIdAndValue(setId, value);

            return SendExpectLongAsync(token, Commands.ZAdd, setId.ToUtf8Bytes(), score.ToUtf8Bytes(), value);
        }

        ValueTask<long> IRedisNativeClientAsync.HSetAsync(string hashId, byte[] key, byte[] value, CancellationToken token)
            => HSetAsync(hashId.ToUtf8Bytes(), key, value, token);

        internal ValueTask<long> HSetAsync(byte[] hashId, byte[] key, byte[] value, CancellationToken token = default)
        {
            AssertHashIdAndKey(hashId, key);

            return SendExpectLongAsync(token, Commands.HSet, hashId, key, value);
        }

        ValueTask<string> IRedisNativeClientAsync.RandomKeyAsync(CancellationToken token)
            => SendExpectDataAsync(token, Commands.RandomKey).FromUtf8BytesAsync();

        ValueTask IRedisNativeClientAsync.RenameAsync(string oldKeyName, string newKeyName, CancellationToken token)
        {
            CheckRenameKeys(oldKeyName, newKeyName);
            return SendExpectSuccessAsync(token, Commands.Rename, oldKeyName.ToUtf8Bytes(), newKeyName.ToUtf8Bytes());
        }

        ValueTask<bool> IRedisNativeClientAsync.RenameNxAsync(string oldKeyName, string newKeyName, CancellationToken token)
        {
            CheckRenameKeys(oldKeyName, newKeyName);
            return SendExpectLongAsync(token, Commands.RenameNx, oldKeyName.ToUtf8Bytes(), newKeyName.ToUtf8Bytes()).IsSuccessAsync();
        }

        ValueTask IRedisNativeClientAsync.MSetAsync(byte[][] keys, byte[][] values, CancellationToken token)
        {
            var keysAndValues = MergeCommandWithKeysAndValues(Commands.MSet, keys, values);

            return SendExpectSuccessAsync(token, keysAndValues);
        }


        ValueTask IRedisNativeClientAsync.MSetAsync(string[] keys, byte[][] values, CancellationToken token)
            => ((IRedisNativeClientAsync)this).MSetAsync(keys.ToMultiByteArray(), values, token);

        ValueTask IRedisNativeClientAsync.SelectAsync(long db, CancellationToken token)
        {
            this.db = db;
            return SendExpectSuccessAsync(token, Commands.Select, db.ToUtf8Bytes());
        }

        ValueTask<long> IRedisNativeClientAsync.DelAsync(string[] keys, CancellationToken token)
        {
            AssertNotNull(keys, nameof(keys));

            var cmdWithArgs = MergeCommandWithArgs(Commands.Del, keys);
            return SendExpectLongAsync(token, cmdWithArgs);
        }

        ValueTask<bool> IRedisNativeClientAsync.ExpireAsync(string key, int seconds, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectLongAsync(token, Commands.Expire, key.ToUtf8Bytes(), seconds.ToUtf8Bytes()).IsSuccessAsync();
        }

        ValueTask<bool> IRedisNativeClientAsync.PExpireAsync(string key, long ttlMs, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectLongAsync(token, Commands.PExpire, key.ToUtf8Bytes(), ttlMs.ToUtf8Bytes()).IsSuccessAsync();
        }

        ValueTask<bool> IRedisNativeClientAsync.ExpireAtAsync(string key, long unixTime, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectLongAsync(token, Commands.ExpireAt, key.ToUtf8Bytes(), unixTime.ToUtf8Bytes()).IsSuccessAsync();
        }

        ValueTask<bool> IRedisNativeClientAsync.PExpireAtAsync(string key, long unixTimeMs, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectLongAsync(token, Commands.PExpireAt, key.ToUtf8Bytes(), unixTimeMs.ToUtf8Bytes()).IsSuccessAsync();
        }

        ValueTask<long> IRedisNativeClientAsync.TtlAsync(string key, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectLongAsync(token, Commands.Ttl, key.ToUtf8Bytes());
        }

        ValueTask<long> IRedisNativeClientAsync.PTtlAsync(string key, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectLongAsync(token, Commands.PTtl, key.ToUtf8Bytes());
        }

        ValueTask<bool> IRedisNativeClientAsync.PingAsync(CancellationToken token)
            => IsString(SendExpectCodeAsync(token, Commands.Ping), "PONG");

        private static ValueTask<bool> IsString(ValueTask<string> pending, string expected)
        {
            return pending.IsCompletedSuccessfully ? (pending.Result == expected).AsValueTaskResult()
                : Awaited(pending, expected);

            static async ValueTask<bool> Awaited(ValueTask<string> pending, string expected)
                => await pending.ConfigureAwait(false) == expected;
        }

        ValueTask<string> IRedisNativeClientAsync.EchoAsync(string text, CancellationToken token)
            => SendExpectDataAsync(token, Commands.Echo, text.ToUtf8Bytes()).FromUtf8BytesAsync();

        ValueTask<long> IRedisNativeClientAsync.DbSizeAsync(CancellationToken token)
            => SendExpectLongAsync(token, Commands.DbSize);

        ValueTask<DateTime> IRedisNativeClientAsync.LastSaveAsync(CancellationToken token)
            => SendExpectLongAsync(token, Commands.LastSave).Await(t => t.FromUnixTime());

        ValueTask IRedisNativeClientAsync.SaveAsync(CancellationToken token)
            => SendExpectSuccessAsync(token, Commands.Save);

        ValueTask IRedisNativeClientAsync.BgSaveAsync(CancellationToken token)
            => SendExpectSuccessAsync(token, Commands.BgSave);

        ValueTask IRedisNativeClientAsync.ShutdownAsync(bool noSave, CancellationToken token)
            => noSave
            ? SendWithoutReadAsync(token, Commands.Shutdown, Commands.NoSave)
            : SendWithoutReadAsync(token, Commands.Shutdown);

        ValueTask IRedisNativeClientAsync.BgRewriteAofAsync(CancellationToken token)
            => SendExpectSuccessAsync(token, Commands.BgRewriteAof);

        ValueTask IRedisNativeClientAsync.QuitAsync(CancellationToken token)
            => SendWithoutReadAsync(token, Commands.Quit);

        ValueTask IRedisNativeClientAsync.FlushDbAsync(CancellationToken token)
            => SendExpectSuccessAsync(token, Commands.FlushDb);

        ValueTask IRedisNativeClientAsync.FlushAllAsync(CancellationToken token)
            => SendExpectSuccessAsync(token, Commands.FlushAll);

        ValueTask IRedisNativeClientAsync.SlaveOfAsync(string hostname, int port, CancellationToken token)
            => SendExpectSuccessAsync(token, Commands.SlaveOf, hostname.ToUtf8Bytes(), port.ToUtf8Bytes());

        ValueTask IRedisNativeClientAsync.SlaveOfNoOneAsync(CancellationToken token)
            => SendExpectSuccessAsync(token, Commands.SlaveOf, Commands.No, Commands.One);

        ValueTask<byte[][]> IRedisNativeClientAsync.KeysAsync(string pattern, CancellationToken token)
        {
            AssertNotNull(pattern, nameof(pattern));
            return SendExpectMultiDataAsync(token, Commands.Keys, pattern.ToUtf8Bytes());
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.MGetAsync(string[] keys, CancellationToken token)
        {
            AssertNotNull(keys, nameof(keys));
            if (keys.Length == 0)
                throw new ArgumentException("keys");

            var cmdWithArgs = MergeCommandWithArgs(Commands.MGet, keys);

            return SendExpectMultiDataAsync(token, cmdWithArgs);
        }

        ValueTask IRedisNativeClientAsync.SetExAsync(string key, int expireInSeconds, byte[] value, CancellationToken token)
        {
            AssertNotNull(key);
            value ??= TypeConstants.EmptyByteArray;

            if (value.Length > OneGb)
                throw new ArgumentException("value exceeds 1G", nameof(value));

            return SendExpectSuccessAsync(token, Commands.SetEx, key.ToUtf8Bytes(), expireInSeconds.ToUtf8Bytes(), value);
        }

        ValueTask IRedisNativeClientAsync.WatchAsync(string[] keys, CancellationToken token)
        {
            AssertNotNull(keys, nameof(keys));
            if (keys.Length == 0)
                throw new ArgumentException("keys");

            var cmdWithArgs = MergeCommandWithArgs(Commands.Watch, keys);

            return SendExpectCodeAsync(token, cmdWithArgs).Await();
        }

        ValueTask IRedisNativeClientAsync.UnWatchAsync(CancellationToken token)
            => SendExpectCodeAsync(token, Commands.UnWatch).Await();

        ValueTask<long> IRedisNativeClientAsync.AppendAsync(string key, byte[] value, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectLongAsync(token, Commands.Append, key.ToUtf8Bytes(), value);
        }

        ValueTask<byte[]> IRedisNativeClientAsync.GetRangeAsync(string key, int fromIndex, int toIndex, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectDataAsync(token, Commands.GetRange, key.ToUtf8Bytes(), fromIndex.ToUtf8Bytes(), toIndex.ToUtf8Bytes());
        }

        ValueTask<long> IRedisNativeClientAsync.SetRangeAsync(string key, int offset, byte[] value, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectLongAsync(token, Commands.SetRange, key.ToUtf8Bytes(), offset.ToUtf8Bytes(), value);
        }

        ValueTask<long> IRedisNativeClientAsync.GetBitAsync(string key, int offset, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectLongAsync(token, Commands.GetBit, key.ToUtf8Bytes(), offset.ToUtf8Bytes());
        }

        ValueTask<long> IRedisNativeClientAsync.SetBitAsync(string key, int offset, int value, CancellationToken token)
        {
            AssertNotNull(key);
            if (value > 1 || value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "value is out of range");
            return SendExpectLongAsync(token, Commands.SetBit, key.ToUtf8Bytes(), offset.ToUtf8Bytes(), value.ToUtf8Bytes());
        }

        ValueTask<bool> IRedisNativeClientAsync.PersistAsync(string key, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectLongAsync(token, Commands.Persist, key.ToUtf8Bytes()).IsSuccessAsync();
        }

        ValueTask IRedisNativeClientAsync.PSetExAsync(string key, long expireInMs, byte[] value, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectSuccessAsync(token, Commands.PSetEx, key.ToUtf8Bytes(), expireInMs.ToUtf8Bytes(), value);
        }

        ValueTask<long> IRedisNativeClientAsync.SetNXAsync(string key, byte[] value, CancellationToken token)
        {
            AssertNotNull(key);
            value ??= TypeConstants.EmptyByteArray;

            if (value.Length > OneGb)
                throw new ArgumentException("value exceeds 1G", "value");

            return SendExpectLongAsync(token, Commands.SetNx, key.ToUtf8Bytes(), value);
        }

        ValueTask<byte[]> IRedisNativeClientAsync.SPopAsync(string setId, CancellationToken token)
        {
            AssertNotNull(setId, nameof(setId));
            return SendExpectDataAsync(token, Commands.SPop, setId.ToUtf8Bytes());
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.SPopAsync(string setId, int count, CancellationToken token)
        {
            AssertNotNull(setId, nameof(setId));
            return SendExpectMultiDataAsync(token, Commands.SPop, setId.ToUtf8Bytes(), count.ToUtf8Bytes());
        }

        ValueTask IRedisNativeClientAsync.SlowlogResetAsync(CancellationToken token)
            => SendExpectSuccessAsync(token, Commands.Slowlog, "RESET".ToUtf8Bytes());

        ValueTask<object[]> IRedisNativeClientAsync.SlowlogGetAsync(int? top, CancellationToken token)
        {
            if (top.HasValue)
                return SendExpectDeeplyNestedMultiDataAsync(token, Commands.Slowlog, Commands.Get, top.Value.ToUtf8Bytes());
            else
                return SendExpectDeeplyNestedMultiDataAsync(token, Commands.Slowlog, Commands.Get);
        }

        ValueTask<long> IRedisNativeClientAsync.ZCardAsync(string setId, CancellationToken token)
        {
            AssertNotNull(setId, nameof(setId));
            return SendExpectLongAsync(token, Commands.ZCard, setId.ToUtf8Bytes());
        }

        ValueTask<long> IRedisNativeClientAsync.ZCountAsync(string setId, double min, double max, CancellationToken token)
        {
            AssertNotNull(setId, nameof(setId));
            return SendExpectLongAsync(token, Commands.ZCount, setId.ToUtf8Bytes(), min.ToUtf8Bytes(), max.ToUtf8Bytes());
        }

        ValueTask<double> IRedisNativeClientAsync.ZScoreAsync(string setId, byte[] value, CancellationToken token)
        {
            AssertNotNull(setId, nameof(setId));
            return SendExpectDoubleAsync(token, Commands.ZScore, setId.ToUtf8Bytes(), value);
        }

        protected ValueTask<RedisData> RawCommandAsync(CancellationToken token, params object[] cmdWithArgs)
        {
            var byteArgs = new List<byte[]>();

            foreach (var arg in cmdWithArgs)
            {
                if (arg == null)
                {
                    byteArgs.Add(TypeConstants.EmptyByteArray);
                    continue;
                }

                if (arg is byte[] bytes)
                {
                    byteArgs.Add(bytes);
                }
                else if (arg.GetType().IsUserType())
                {
                    var json = arg.ToJson();
                    byteArgs.Add(json.ToUtf8Bytes());
                }
                else
                {
                    var str = arg.ToString();
                    byteArgs.Add(str.ToUtf8Bytes());
                }
            }

            return SendExpectComplexResponseAsync(token, byteArgs.ToArray());
        }

        ValueTask<Dictionary<string, string>> IRedisNativeClientAsync.InfoAsync(CancellationToken token)
            => SendExpectStringAsync(token, Commands.Info).Await(ParseInfoResult);

        ValueTask<byte[][]> IRedisNativeClientAsync.ZRangeByLexAsync(string setId, string min, string max, int? skip, int? take, CancellationToken token)
            => SendExpectMultiDataAsync(token, GetZRangeByLexArgs(setId, min, max, skip, take));

        ValueTask<long> IRedisNativeClientAsync.ZLexCountAsync(string setId, string min, string max, CancellationToken token)
        {
            AssertNotNull(setId, nameof(setId));

            return SendExpectLongAsync(token,
                Commands.ZLexCount, setId.ToUtf8Bytes(), min.ToUtf8Bytes(), max.ToUtf8Bytes());
        }

        ValueTask<long> IRedisNativeClientAsync.ZRemRangeByLexAsync(string setId, string min, string max, CancellationToken token)
        {
            AssertNotNull(setId, nameof(setId));

            return SendExpectLongAsync(token,
                Commands.ZRemRangeByLex, setId.ToUtf8Bytes(), min.ToUtf8Bytes(), max.ToUtf8Bytes());
        }

        ValueTask<string> IRedisNativeClientAsync.CalculateSha1Async(string luaBody, CancellationToken token)
        {
            AssertNotNull(luaBody, nameof(luaBody));

            byte[] buffer = Encoding.UTF8.GetBytes(luaBody);
            return BitConverter.ToString(buffer.ToSha1Hash()).Replace("-", "").AsValueTaskResult();
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.ScriptExistsAsync(byte[][] sha1Refs, CancellationToken token)
        {
            var keysAndValues = MergeCommandWithArgs(Commands.Script, Commands.Exists, sha1Refs);
            return SendExpectMultiDataAsync(token, keysAndValues);
        }

        ValueTask IRedisNativeClientAsync.ScriptFlushAsync(CancellationToken token)
            => SendExpectSuccessAsync(token, Commands.Script, Commands.Flush);

        ValueTask IRedisNativeClientAsync.ScriptKillAsync(CancellationToken token)
            => SendExpectSuccessAsync(token, Commands.Script, Commands.Kill);

        ValueTask<byte[]> IRedisNativeClientAsync.ScriptLoadAsync(string body, CancellationToken token)
        {
            AssertNotNull(body, nameof(body));

            var cmdArgs = MergeCommandWithArgs(Commands.Script, Commands.Load, body.ToUtf8Bytes());
            return SendExpectDataAsync(token, cmdArgs);
        }

        ValueTask<long> IRedisNativeClientAsync.StrLenAsync(string key, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectLongAsync(token, Commands.StrLen, key.ToUtf8Bytes());
        }

        ValueTask<long> IRedisNativeClientAsync.LLenAsync(string listId, CancellationToken token)
        {
            AssertNotNull(listId, nameof(listId));
            return SendExpectLongAsync(token, Commands.LLen, listId.ToUtf8Bytes());
        }

        ValueTask<long> IRedisNativeClientAsync.SCardAsync(string setId, CancellationToken token)
        {
            AssertNotNull(setId, nameof(setId));
            return SendExpectLongAsync(token, Commands.SCard, setId.ToUtf8Bytes());
        }

        ValueTask<long> IRedisNativeClientAsync.HLenAsync(string hashId, CancellationToken token)
        {
            AssertNotNull(hashId, nameof(hashId));
            return SendExpectLongAsync(token, Commands.HLen, hashId.ToUtf8Bytes());
        }

        ValueTask<RedisData> IRedisNativeClientAsync.EvalCommandAsync(string luaBody, int numberKeysInArgs, byte[][] keys, CancellationToken token)
        {
            AssertNotNull(luaBody, nameof(luaBody));

            var cmdArgs = MergeCommandWithArgs(Commands.Eval, luaBody.ToUtf8Bytes(), keys.PrependInt(numberKeysInArgs));
            return RawCommandAsync(token, cmdArgs);
        }

        ValueTask<RedisData> IRedisNativeClientAsync.EvalShaCommandAsync(string sha1, int numberKeysInArgs, byte[][] keys, CancellationToken token)
        {
            AssertNotNull(sha1, nameof(sha1));

            var cmdArgs = MergeCommandWithArgs(Commands.EvalSha, sha1.ToUtf8Bytes(), keys.PrependInt(numberKeysInArgs));
            return RawCommandAsync(token, cmdArgs);
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.EvalAsync(string luaBody, int numberOfKeys, byte[][] keysAndArgs, CancellationToken token)
        {
            AssertNotNull(luaBody, nameof(luaBody));

            var cmdArgs = MergeCommandWithArgs(Commands.Eval, luaBody.ToUtf8Bytes(), keysAndArgs.PrependInt(numberOfKeys));
            return SendExpectMultiDataAsync(token, cmdArgs);
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.EvalShaAsync(string sha1, int numberOfKeys, byte[][] keysAndArgs, CancellationToken token)
        {
            AssertNotNull(sha1, nameof(sha1));

            var cmdArgs = MergeCommandWithArgs(Commands.EvalSha, sha1.ToUtf8Bytes(), keysAndArgs.PrependInt(numberOfKeys));
            return SendExpectMultiDataAsync(token, cmdArgs);
        }

        ValueTask<long> IRedisNativeClientAsync.EvalIntAsync(string luaBody, int numberOfKeys, byte[][] keysAndArgs, CancellationToken token)
        {
            AssertNotNull(luaBody, nameof(luaBody));

            var cmdArgs = MergeCommandWithArgs(Commands.Eval, luaBody.ToUtf8Bytes(), keysAndArgs.PrependInt(numberOfKeys));
            return SendExpectLongAsync(token, cmdArgs);
        }

        ValueTask<long> IRedisNativeClientAsync.EvalShaIntAsync(string sha1, int numberOfKeys, byte[][] keysAndArgs, CancellationToken token)
        {
            AssertNotNull(sha1, nameof(sha1));

            var cmdArgs = MergeCommandWithArgs(Commands.EvalSha, sha1.ToUtf8Bytes(), keysAndArgs.PrependInt(numberOfKeys));
            return SendExpectLongAsync(token, cmdArgs);
        }

        ValueTask<string> IRedisNativeClientAsync.EvalStrAsync(string luaBody, int numberOfKeys, byte[][] keysAndArgs, CancellationToken token)
        {
            AssertNotNull(luaBody, nameof(luaBody));

            var cmdArgs = MergeCommandWithArgs(Commands.Eval, luaBody.ToUtf8Bytes(), keysAndArgs.PrependInt(numberOfKeys));
            return SendExpectDataAsync(token, cmdArgs).FromUtf8BytesAsync();
        }

        ValueTask<string> IRedisNativeClientAsync.EvalShaStrAsync(string sha1, int numberOfKeys, byte[][] keysAndArgs, CancellationToken token)
        {
            AssertNotNull(sha1, nameof(sha1));

            var cmdArgs = MergeCommandWithArgs(Commands.EvalSha, sha1.ToUtf8Bytes(), keysAndArgs.PrependInt(numberOfKeys));
            return SendExpectDataAsync(token, cmdArgs).FromUtf8BytesAsync();
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.SMembersAsync(string setId, CancellationToken token)
            => SendExpectMultiDataAsync(token, Commands.SMembers, setId.ToUtf8Bytes());

        ValueTask<long> IRedisNativeClientAsync.SAddAsync(string setId, byte[][] values, CancellationToken token)
        {
            AssertNotNull(setId, nameof(setId));
            AssertNotNull(values, nameof(values));
            if (values.Length == 0)
                throw new ArgumentException(nameof(values));

            var cmdWithArgs = MergeCommandWithArgs(Commands.SAdd, setId.ToUtf8Bytes(), values);
            return SendExpectLongAsync(token, cmdWithArgs);
        }

        ValueTask<long> IRedisNativeClientAsync.SRemAsync(string setId, byte[] value, CancellationToken token)
        {
            AssertSetIdAndValue(setId, value);
            return SendExpectLongAsync(token, Commands.SRem, setId.ToUtf8Bytes(), value);
        }

        ValueTask<long> IRedisNativeClientAsync.IncrByAsync(string key, long count, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectLongAsync(token, Commands.IncrBy, key.ToUtf8Bytes(), count.ToUtf8Bytes());
        }

        ValueTask<double> IRedisNativeClientAsync.IncrByFloatAsync(string key, double incrBy, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectDoubleAsync(token, Commands.IncrByFloat, key.ToUtf8Bytes(), incrBy.ToUtf8Bytes());
        }

        ValueTask<long> IRedisNativeClientAsync.IncrAsync(string key, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectLongAsync(token, Commands.Incr, key.ToUtf8Bytes());
        }

        ValueTask<long> IRedisNativeClientAsync.DecrAsync(string key, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectLongAsync(token, Commands.Decr, key.ToUtf8Bytes());
        }

        ValueTask<long> IRedisNativeClientAsync.DecrByAsync(string key, long count, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectLongAsync(token, Commands.DecrBy, key.ToUtf8Bytes(), count.ToUtf8Bytes());
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.ConfigGetAsync(string pattern, CancellationToken token)
            => SendExpectMultiDataAsync(token, Commands.Config, Commands.Get, pattern.ToUtf8Bytes());

        ValueTask IRedisNativeClientAsync.ConfigSetAsync(string item, byte[] value, CancellationToken token)
            => SendExpectSuccessAsync(token, Commands.Config, Commands.Set, item.ToUtf8Bytes(), value);

        ValueTask IRedisNativeClientAsync.ConfigResetStatAsync(CancellationToken token)
            => SendExpectSuccessAsync(token, Commands.Config, Commands.ResetStat);

        ValueTask IRedisNativeClientAsync.ConfigRewriteAsync(CancellationToken token)
            => SendExpectSuccessAsync(token, Commands.Config, Commands.Rewrite);

        ValueTask IRedisNativeClientAsync.DebugSegfaultAsync(CancellationToken token)
            => SendExpectSuccessAsync(token, Commands.Debug, Commands.Segfault);

        ValueTask<byte[]> IRedisNativeClientAsync.DumpAsync(string key, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectDataAsync(token, Commands.Dump, key.ToUtf8Bytes());
        }

        ValueTask<byte[]> IRedisNativeClientAsync.RestoreAsync(string key, long expireMs, byte[] dumpValue, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectDataAsync(token, Commands.Restore, key.ToUtf8Bytes(), expireMs.ToUtf8Bytes(), dumpValue);
        }

        ValueTask IRedisNativeClientAsync.MigrateAsync(string host, int port, string key, int destinationDb, long timeoutMs, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectSuccessAsync(token, Commands.Migrate, host.ToUtf8Bytes(), port.ToUtf8Bytes(), key.ToUtf8Bytes(), destinationDb.ToUtf8Bytes(), timeoutMs.ToUtf8Bytes());
        }

        ValueTask<bool> IRedisNativeClientAsync.MoveAsync(string key, int db, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectLongAsync(token, Commands.Move, key.ToUtf8Bytes(), db.ToUtf8Bytes()).IsSuccessAsync();
        }

        ValueTask<long> IRedisNativeClientAsync.ObjectIdleTimeAsync(string key, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectLongAsync(token, Commands.Object, Commands.IdleTime, key.ToUtf8Bytes());
        }

        async ValueTask<RedisText> IRedisNativeClientAsync.RoleAsync(CancellationToken token)
            => (await SendExpectComplexResponseAsync(token, Commands.Role).ConfigureAwait(false)).ToRedisText();

        ValueTask<RedisData> IRedisNativeClientAsync.RawCommandAsync(object[] cmdWithArgs, CancellationToken token)
            => SendExpectComplexResponseAsync(token, PrepareRawCommand(cmdWithArgs));

        ValueTask<RedisData> IRedisNativeClientAsync.RawCommandAsync(byte[][] cmdWithBinaryArgs, CancellationToken token)
            => SendExpectComplexResponseAsync(token, cmdWithBinaryArgs);

        ValueTask<string> IRedisNativeClientAsync.ClientGetNameAsync(CancellationToken token)
            => SendExpectStringAsync(token, Commands.Client, Commands.GetName);

        ValueTask IRedisNativeClientAsync.ClientSetNameAsync(string name, CancellationToken token)
        {
            ClientValidateName(name);
            return SendExpectSuccessAsync(token, Commands.Client, Commands.SetName, name.ToUtf8Bytes());
        }

        ValueTask IRedisNativeClientAsync.ClientKillAsync(string clientAddr, CancellationToken token)
            => SendExpectSuccessAsync(token, Commands.Client, Commands.Kill, clientAddr.ToUtf8Bytes());

        ValueTask<long> IRedisNativeClientAsync.ClientKillAsync(string addr, string id, string type, string skipMe, CancellationToken token)
            => SendExpectLongAsync(token, ClientKillPrepareArgs(addr, id, type, skipMe));

        ValueTask<byte[]> IRedisNativeClientAsync.ClientListAsync(CancellationToken token)
            => SendExpectDataAsync(token, Commands.Client, Commands.List);

        ValueTask IRedisNativeClientAsync.ClientPauseAsync(int timeOutMs, CancellationToken token)
            => SendExpectSuccessAsync(token, Commands.Client, Commands.Pause, timeOutMs.ToUtf8Bytes());

        ValueTask<bool> IRedisNativeClientAsync.MSetNxAsync(byte[][] keys, byte[][] values, CancellationToken token)
        {
            var keysAndValues = MergeCommandWithKeysAndValues(Commands.MSet, keys, values);
            return SendExpectLongAsync(token, keysAndValues).IsSuccessAsync();
        }

        ValueTask<bool> IRedisNativeClientAsync.MSetNxAsync(string[] keys, byte[][] values, CancellationToken token)
            => AsAsync().MSetNxAsync(keys.ToMultiByteArray(), values, token);

        ValueTask<byte[]> IRedisNativeClientAsync.GetSetAsync(string key, byte[] value, CancellationToken token)
        {
            GetSetAssertArgs(key, ref value);
            return SendExpectDataAsync(token, Commands.GetSet, key.ToUtf8Bytes(), value);
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.MGetAsync(byte[][] keys, CancellationToken token)
            => SendExpectMultiDataAsync(token, MGetPrepareArgs(keys));

        ValueTask<ScanResult> IRedisNativeClientAsync.SScanAsync(string setId, ulong cursor, int count, string match, CancellationToken token)
        {
            if (match == null)
            {
                return SendExpectScanResultAsync(token, Commands.SScan,
                    setId.ToUtf8Bytes(), cursor.ToUtf8Bytes(),
                    Commands.Count, count.ToUtf8Bytes());
            }

            return SendExpectScanResultAsync(token, Commands.SScan,
                setId.ToUtf8Bytes(), cursor.ToUtf8Bytes(),
                Commands.Match, match.ToUtf8Bytes(),
                Commands.Count, count.ToUtf8Bytes());
        }

        ValueTask<ScanResult> IRedisNativeClientAsync.ZScanAsync(string setId, ulong cursor, int count, string match, CancellationToken token)
        {
            if (match == null)
            {
                return SendExpectScanResultAsync(token, Commands.ZScan,
                    setId.ToUtf8Bytes(), cursor.ToUtf8Bytes(),
                    Commands.Count, count.ToUtf8Bytes());
            }

            return SendExpectScanResultAsync(token, Commands.ZScan,
                setId.ToUtf8Bytes(), cursor.ToUtf8Bytes(),
                Commands.Match, match.ToUtf8Bytes(),
                Commands.Count, count.ToUtf8Bytes());
        }

        ValueTask<ScanResult> IRedisNativeClientAsync.HScanAsync(string hashId, ulong cursor, int count, string match, CancellationToken token)
        {
            if (match == null)
            {
                return SendExpectScanResultAsync(token, Commands.HScan,
                    hashId.ToUtf8Bytes(), cursor.ToUtf8Bytes(),
                    Commands.Count, count.ToUtf8Bytes());
            }

            return SendExpectScanResultAsync(token, Commands.HScan,
                hashId.ToUtf8Bytes(), cursor.ToUtf8Bytes(),
                Commands.Match, match.ToUtf8Bytes(),
                Commands.Count, count.ToUtf8Bytes());
        }

        ValueTask<bool> IRedisNativeClientAsync.PfAddAsync(string key, byte[][] elements, CancellationToken token)
        {
            var cmdWithArgs = MergeCommandWithArgs(Commands.PfAdd, key.ToUtf8Bytes(), elements);
            return SendExpectLongAsync(token, cmdWithArgs).IsSuccessAsync();
        }

        ValueTask<long> IRedisNativeClientAsync.PfCountAsync(string key, CancellationToken token)
        {
            var cmdWithArgs = MergeCommandWithArgs(Commands.PfCount, key.ToUtf8Bytes());
            return SendExpectLongAsync(token, cmdWithArgs);
        }

        ValueTask IRedisNativeClientAsync.PfMergeAsync(string toKeyId, string[] fromKeys, CancellationToken token)
        {
            var fromKeyBytes = fromKeys.Map(x => x.ToUtf8Bytes()).ToArray();
            var cmdWithArgs = MergeCommandWithArgs(Commands.PfMerge, toKeyId.ToUtf8Bytes(), fromKeyBytes);
            return SendExpectSuccessAsync(token, cmdWithArgs);
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.SortAsync(string listOrSetId, SortOptions sortOptions, CancellationToken token)
            => SendExpectMultiDataAsync(token, SortPrepareArgs(listOrSetId, sortOptions));

        ValueTask<byte[][]> IRedisNativeClientAsync.LRangeAsync(string listId, int startingFrom, int endingAt, CancellationToken token)
        {
            AssertNotNull(listId, nameof(listId));
            return SendExpectMultiDataAsync(token, Commands.LRange, listId.ToUtf8Bytes(), startingFrom.ToUtf8Bytes(), endingAt.ToUtf8Bytes());
        }

        ValueTask<long> IRedisNativeClientAsync.RPushXAsync(string listId, byte[] value, CancellationToken token)
        {
            AssertListIdAndValue(listId, value);
            return SendExpectLongAsync(token, Commands.RPush, listId.ToUtf8Bytes(), value);
        }

        ValueTask<long> IRedisNativeClientAsync.LPushAsync(string listId, byte[] value, CancellationToken token)
        {
            AssertListIdAndValue(listId, value);
            return SendExpectLongAsync(token, Commands.LPush, listId.ToUtf8Bytes(), value);
        }

        ValueTask<long> IRedisNativeClientAsync.LPushXAsync(string listId, byte[] value, CancellationToken token)
        {
            AssertListIdAndValue(listId, value);
            return SendExpectLongAsync(token, Commands.LPushX, listId.ToUtf8Bytes(), value);
        }

        ValueTask IRedisNativeClientAsync.LTrimAsync(string listId, int keepStartingFrom, int keepEndingAt, CancellationToken token)
        {
            AssertNotNull(listId, nameof(listId));
            return SendExpectSuccessAsync(token, Commands.LTrim, listId.ToUtf8Bytes(), keepStartingFrom.ToUtf8Bytes(), keepEndingAt.ToUtf8Bytes());
        }

        ValueTask<long> IRedisNativeClientAsync.LRemAsync(string listId, int removeNoOfMatches, byte[] value, CancellationToken token)
        {
            AssertNotNull(listId, nameof(listId));
            return SendExpectLongAsync(token, Commands.LRem, listId.ToUtf8Bytes(), removeNoOfMatches.ToUtf8Bytes(), value);
        }

        ValueTask<byte[]> IRedisNativeClientAsync.LIndexAsync(string listId, int listIndex, CancellationToken token)
        {
            AssertNotNull(listId, nameof(listId));
            return SendExpectDataAsync(token, Commands.LIndex, listId.ToUtf8Bytes(), listIndex.ToUtf8Bytes());
        }

        ValueTask IRedisNativeClientAsync.LInsertAsync(string listId, bool insertBefore, byte[] pivot, byte[] value, CancellationToken token)
        {
            AssertNotNull(listId, nameof(listId));
            var position = insertBefore ? Commands.Before : Commands.After;
            return SendExpectSuccessAsync(token, Commands.LInsert, listId.ToUtf8Bytes(), position, pivot, value);
        }

        ValueTask IRedisNativeClientAsync.LSetAsync(string listId, int listIndex, byte[] value, CancellationToken token)
        {
            AssertNotNull(listId, nameof(listId));
            return SendExpectSuccessAsync(token, Commands.LSet, listId.ToUtf8Bytes(), listIndex.ToUtf8Bytes(), value);
        }

        ValueTask<byte[]> IRedisNativeClientAsync.LPopAsync(string listId, CancellationToken token)
        {
            AssertNotNull(listId, nameof(listId));
            return SendExpectDataAsync(token, Commands.LPop, listId.ToUtf8Bytes());
        }

        ValueTask<byte[]> IRedisNativeClientAsync.RPopAsync(string listId, CancellationToken token)
        {
            AssertNotNull(listId, nameof(listId));
            return SendExpectDataAsync(token, Commands.RPop, listId.ToUtf8Bytes());
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.BLPopAsync(string listId, int timeOutSecs, CancellationToken token)
        {
            AssertNotNull(listId, nameof(listId));
            return SendExpectMultiDataAsync(token, Commands.BLPop, listId.ToUtf8Bytes(), timeOutSecs.ToUtf8Bytes());
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.BLPopAsync(string[] listIds, int timeOutSecs, CancellationToken token)
        {
            AssertNotNull(listIds, nameof(listIds));
            var args = new List<byte[]> { Commands.BLPop };
            args.AddRange(listIds.Select(listId => listId.ToUtf8Bytes()));
            args.Add(timeOutSecs.ToUtf8Bytes());
            return SendExpectMultiDataAsync(token, args.ToArray());
        }

        async ValueTask<byte[]> IRedisNativeClientAsync.BLPopValueAsync(string listId, int timeOutSecs, CancellationToken token)
        {
            var blockingResponse = await AsAsync().BLPopAsync(new[] { listId }, timeOutSecs, token).ConfigureAwait(false);
            return blockingResponse.Length == 0
                ? null
                : blockingResponse[1];
        }

        async ValueTask<byte[][]> IRedisNativeClientAsync.BLPopValueAsync(string[] listIds, int timeOutSecs, CancellationToken token)
        {
            var blockingResponse = await AsAsync().BLPopAsync(listIds, timeOutSecs, token).ConfigureAwait(false);
            return blockingResponse.Length == 0
                ? null
                : blockingResponse;
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.BRPopAsync(string listId, int timeOutSecs, CancellationToken token)
        {
            AssertNotNull(listId, nameof(listId));
            return SendExpectMultiDataAsync(token, Commands.BRPop, listId.ToUtf8Bytes(), timeOutSecs.ToUtf8Bytes());
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.BRPopAsync(string[] listIds, int timeOutSecs, CancellationToken token)
        {
            AssertNotNull(listIds, nameof(listIds));
            var args = new List<byte[]> { Commands.BRPop };
            args.AddRange(listIds.Select(listId => listId.ToUtf8Bytes()));
            args.Add(timeOutSecs.ToUtf8Bytes());
            return SendExpectMultiDataAsync(token, args.ToArray());
        }

        ValueTask<byte[]> IRedisNativeClientAsync.RPopLPushAsync(string fromListId, string toListId, CancellationToken token)
        {
            AssertNotNull(fromListId, nameof(fromListId));
            AssertNotNull(toListId, nameof(toListId));
            return SendExpectDataAsync(token, Commands.RPopLPush, fromListId.ToUtf8Bytes(), toListId.ToUtf8Bytes());
        }

        async ValueTask<byte[]> IRedisNativeClientAsync.BRPopValueAsync(string listId, int timeOutSecs, CancellationToken token)
        {
            var blockingResponse = await AsAsync().BRPopAsync(new[] { listId }, timeOutSecs, token).ConfigureAwait(false);
            return blockingResponse.Length == 0
                ? null
                : blockingResponse[1];
        }

        async ValueTask<byte[][]> IRedisNativeClientAsync.BRPopValueAsync(string[] listIds, int timeOutSecs, CancellationToken token)
        {
            var blockingResponse = await AsAsync().BRPopAsync(listIds, timeOutSecs, token).ConfigureAwait(false);
            return blockingResponse.Length == 0
                ? null
                : blockingResponse;
        }

        async ValueTask<byte[]> IRedisNativeClientAsync.BRPopLPushAsync(string fromListId, string toListId, int timeOutSecs, CancellationToken token)
        {
            AssertNotNull(fromListId, nameof(fromListId));
            AssertNotNull(toListId, nameof(toListId));
            byte[][] result = await SendExpectMultiDataAsync(token, Commands.BRPopLPush, fromListId.ToUtf8Bytes(), toListId.ToUtf8Bytes(), timeOutSecs.ToUtf8Bytes());
            return result.Length == 0 ? null : result[1];
        }

        ValueTask IRedisNativeClientAsync.SMoveAsync(string fromSetId, string toSetId, byte[] value, CancellationToken token)
        {
            AssertNotNull(fromSetId, nameof(fromSetId));
            AssertNotNull(toSetId, nameof(toSetId));
            return SendExpectSuccessAsync(token, Commands.SMove, fromSetId.ToUtf8Bytes(), toSetId.ToUtf8Bytes(), value);
        }

        ValueTask<long> IRedisNativeClientAsync.SIsMemberAsync(string setId, byte[] value, CancellationToken token)
        {
            AssertNotNull(setId, nameof(setId));
            return SendExpectLongAsync(token, Commands.SIsMember, setId.ToUtf8Bytes(), value);
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.SInterAsync(string[] setIds, CancellationToken token)
        {
            var cmdWithArgs = MergeCommandWithArgs(Commands.SInter, setIds);
            return SendExpectMultiDataAsync(token, cmdWithArgs);
        }

        ValueTask IRedisNativeClientAsync.SInterStoreAsync(string intoSetId, string[] setIds, CancellationToken token)
        {
            var setIdsList = new List<string>(setIds);
            setIdsList.Insert(0, intoSetId);

            var cmdWithArgs = MergeCommandWithArgs(Commands.SInterStore, setIdsList.ToArray());
            return SendExpectSuccessAsync(token, cmdWithArgs);
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.SUnionAsync(string[] setIds, CancellationToken token)
        {
            var cmdWithArgs = MergeCommandWithArgs(Commands.SUnion, setIds);
            return SendExpectMultiDataAsync(token, cmdWithArgs);
        }

        ValueTask IRedisNativeClientAsync.SUnionStoreAsync(string intoSetId, string[] setIds, CancellationToken token)
        {
            var setIdsList = new List<string>(setIds);
            setIdsList.Insert(0, intoSetId);

            var cmdWithArgs = MergeCommandWithArgs(Commands.SUnionStore, setIdsList.ToArray());
            return SendExpectSuccessAsync(token, cmdWithArgs);
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.SDiffAsync(string fromSetId, string[] withSetIds, CancellationToken token)
        {
            var setIdsList = new List<string>(withSetIds);
            setIdsList.Insert(0, fromSetId);

            var cmdWithArgs = MergeCommandWithArgs(Commands.SDiff, setIdsList.ToArray());
            return SendExpectMultiDataAsync(token, cmdWithArgs);
        }

        ValueTask IRedisNativeClientAsync.SDiffStoreAsync(string intoSetId, string fromSetId, string[] withSetIds, CancellationToken token)
        {
            var setIdsList = new List<string>(withSetIds);
            setIdsList.Insert(0, fromSetId);
            setIdsList.Insert(0, intoSetId);

            var cmdWithArgs = MergeCommandWithArgs(Commands.SDiffStore, setIdsList.ToArray());
            return SendExpectSuccessAsync(token, cmdWithArgs);
        }

        ValueTask<byte[]> IRedisNativeClientAsync.SRandMemberAsync(string setId, CancellationToken token)
            => SendExpectDataAsync(token, Commands.SRandMember, setId.ToUtf8Bytes());

        ValueTask<long> IRedisNativeClientAsync.ZRemAsync(string setId, byte[] value, CancellationToken token)
        {
            AssertSetIdAndValue(setId, value);
            return SendExpectLongAsync(token, Commands.ZRem, setId.ToUtf8Bytes(), value);
        }

        ValueTask<long> IRedisNativeClientAsync.ZRemAsync(string setId, byte[][] values, CancellationToken token)
        {
            AssertNotNull(setId, nameof(setId));
            AssertNotNull(values, nameof(values));
            if (values.Length == 0)
                throw new ArgumentException("values");

            var cmdWithArgs = MergeCommandWithArgs(Commands.ZRem, setId.ToUtf8Bytes(), values);
            return SendExpectLongAsync(token, cmdWithArgs);
        }

        ValueTask<double> IRedisNativeClientAsync.ZIncrByAsync(string setId, double incrBy, byte[] value, CancellationToken token)
        {
            AssertSetIdAndValue(setId, value);
            return SendExpectDoubleAsync(token, Commands.ZIncrBy, setId.ToUtf8Bytes(), incrBy.ToFastUtf8Bytes(), value);
        }

        ValueTask<double> IRedisNativeClientAsync.ZIncrByAsync(string setId, long incrBy, byte[] value, CancellationToken token)
        {
            AssertSetIdAndValue(setId, value);
            return SendExpectDoubleAsync(token, Commands.ZIncrBy, setId.ToUtf8Bytes(), incrBy.ToUtf8Bytes(), value);
        }

        ValueTask<long> IRedisNativeClientAsync.ZRankAsync(string setId, byte[] value, CancellationToken token)
        {
            AssertSetIdAndValue(setId, value);
            return SendExpectLongAsync(token, Commands.ZRank, setId.ToUtf8Bytes(), value);
        }

        ValueTask<long> IRedisNativeClientAsync.ZRevRankAsync(string setId, byte[] value, CancellationToken token)
        {
            AssertSetIdAndValue(setId, value);
            return SendExpectLongAsync(token, Commands.ZRevRank, setId.ToUtf8Bytes(), value);
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.ZRangeAsync(string setId, int min, int max, CancellationToken token)
            => SendExpectMultiDataAsync(token, Commands.ZRange, setId.ToUtf8Bytes(), min.ToUtf8Bytes(), max.ToUtf8Bytes());

        private ValueTask<byte[][]> GetRangeAsync(byte[] commandBytes, string setId, int min, int max, bool withScores, CancellationToken token)
        {
            var args = GetRangeArgs(commandBytes, setId, min, max, withScores);
            return SendExpectMultiDataAsync(token, args);
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.ZRangeWithScoresAsync(string setId, int min, int max, CancellationToken token)
            => GetRangeAsync(Commands.ZRange, setId, min, max, true, token);

        ValueTask<byte[][]> IRedisNativeClientAsync.ZRevRangeAsync(string setId, int min, int max, CancellationToken token)
            => GetRangeAsync(Commands.ZRevRange, setId, min, max, false, token);

        ValueTask<byte[][]> IRedisNativeClientAsync.ZRevRangeWithScoresAsync(string setId, int min, int max, CancellationToken token)
            => GetRangeAsync(Commands.ZRevRange, setId, min, max, true, token);

        private ValueTask<byte[][]> GetRangeByScoreAsync(byte[] commandBytes,
            string setId, double min, double max, int? skip, int? take, bool withScores, CancellationToken token)
        {
            var args = GetRangeByScoreArgs(commandBytes, setId, min, max, skip, take, withScores);
            return SendExpectMultiDataAsync(token, args);
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.ZRangeByScoreAsync(string setId, double min, double max, int? skip, int? take, CancellationToken token)
            => GetRangeByScoreAsync(Commands.ZRangeByScore, setId, min, max, skip, take, false, token);

        ValueTask<byte[][]> IRedisNativeClientAsync.ZRangeByScoreAsync(string setId, long min, long max, int? skip, int? take, CancellationToken token)
            => GetRangeByScoreAsync(Commands.ZRangeByScore, setId, min, max, skip, take, false, token);

        ValueTask<byte[][]> IRedisNativeClientAsync.ZRangeByScoreWithScoresAsync(string setId, double min, double max, int? skip, int? take, CancellationToken token)
            => GetRangeByScoreAsync(Commands.ZRangeByScore, setId, min, max, skip, take, true, token);

        ValueTask<byte[][]> IRedisNativeClientAsync.ZRangeByScoreWithScoresAsync(string setId, long min, long max, int? skip, int? take, CancellationToken token)
            => GetRangeByScoreAsync(Commands.ZRangeByScore, setId, min, max, skip, take, true, token);

        ValueTask<byte[][]> IRedisNativeClientAsync.ZRevRangeByScoreAsync(string setId, double min, double max, int? skip, int? take, CancellationToken token)
        {
            //Note: http://redis.io/commands/zrevrangebyscore has max, min in the wrong other
            return GetRangeByScoreAsync(Commands.ZRevRangeByScore, setId, max, min, skip, take, false, token);
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.ZRevRangeByScoreAsync(string setId, long min, long max, int? skip, int? take, CancellationToken token)
        {
            //Note: http://redis.io/commands/zrevrangebyscore has max, min in the wrong other
            return GetRangeByScoreAsync(Commands.ZRevRangeByScore, setId, max, min, skip, take, false, token);
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.ZRevRangeByScoreWithScoresAsync(string setId, double min, double max, int? skip, int? take, CancellationToken token)
        {
            //Note: http://redis.io/commands/zrevrangebyscore has max, min in the wrong other
            return GetRangeByScoreAsync(Commands.ZRevRangeByScore, setId, max, min, skip, take, true, token);
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.ZRevRangeByScoreWithScoresAsync(string setId, long min, long max, int? skip, int? take, CancellationToken token)
        {
            //Note: http://redis.io/commands/zrevrangebyscore has max, min in the wrong other
            return GetRangeByScoreAsync(Commands.ZRevRangeByScore, setId, max, min, skip, take, true, token);
        }

        ValueTask<long> IRedisNativeClientAsync.ZRemRangeByRankAsync(string setId, int min, int max, CancellationToken token)
        {
            AssertNotNull(setId, nameof(setId));
            return SendExpectLongAsync(token, Commands.ZRemRangeByRank, setId.ToUtf8Bytes(),
                min.ToUtf8Bytes(), max.ToUtf8Bytes());
        }

        ValueTask<long> IRedisNativeClientAsync.ZRemRangeByScoreAsync(string setId, double fromScore, double toScore, CancellationToken token)
        {
            AssertNotNull(setId, nameof(setId));
            return SendExpectLongAsync(token, Commands.ZRemRangeByScore, setId.ToUtf8Bytes(),
                fromScore.ToFastUtf8Bytes(), toScore.ToFastUtf8Bytes());
        }

        ValueTask<long> IRedisNativeClientAsync.ZRemRangeByScoreAsync(string setId, long fromScore, long toScore, CancellationToken token)
        {
            AssertNotNull(setId, nameof(setId));
            return SendExpectLongAsync(token, Commands.ZRemRangeByScore, setId.ToUtf8Bytes(),
                fromScore.ToUtf8Bytes(), toScore.ToUtf8Bytes());
        }

        ValueTask<long> IRedisNativeClientAsync.ZUnionStoreAsync(string intoSetId, string[] setIds, CancellationToken token)
        {
            var setIdsList = new List<string>(setIds);
            setIdsList.Insert(0, setIds.Length.ToString());
            setIdsList.Insert(0, intoSetId);

            var cmdWithArgs = MergeCommandWithArgs(Commands.ZUnionStore, setIdsList.ToArray());
            return SendExpectLongAsync(token, cmdWithArgs);
        }

        ValueTask<long> IRedisNativeClientAsync.ZInterStoreAsync(string intoSetId, string[] setIds, CancellationToken token)
        {
            var setIdsList = new List<string>(setIds);
            setIdsList.Insert(0, setIds.Length.ToString());
            setIdsList.Insert(0, intoSetId);

            var cmdWithArgs = MergeCommandWithArgs(Commands.ZInterStore, setIdsList.ToArray());
            return SendExpectLongAsync(token, cmdWithArgs);
        }

        internal ValueTask<long> ZInterStoreAsync(string intoSetId, string[] setIds, string[] args, CancellationToken token)
        {
            var totalArgs = new List<string>(setIds);
            totalArgs.Insert(0, setIds.Length.ToString());
            totalArgs.Insert(0, intoSetId);
            totalArgs.AddRange(args);

            var cmdWithArgs = MergeCommandWithArgs(Commands.ZInterStore, totalArgs.ToArray());
            return SendExpectLongAsync(token, cmdWithArgs);
        }

        internal ValueTask<long> ZUnionStoreAsync(string intoSetId, string[] setIds, string[] args, CancellationToken token)
        {
            var totalArgs = new List<string>(setIds);
            totalArgs.Insert(0, setIds.Length.ToString());
            totalArgs.Insert(0, intoSetId);
            totalArgs.AddRange(args);

            var cmdWithArgs = MergeCommandWithArgs(Commands.ZUnionStore, totalArgs.ToArray());
            return SendExpectLongAsync(token, cmdWithArgs);
        }

        ValueTask IRedisNativeClientAsync.HMSetAsync(string hashId, byte[][] keys, byte[][] values, CancellationToken token)
        {
            AssertNotNull(hashId, nameof(hashId));
            var cmdArgs = MergeCommandWithKeysAndValues(Commands.HMSet, hashId.ToUtf8Bytes(), keys, values);
            return SendExpectSuccessAsync(token, cmdArgs);
        }

        ValueTask<long> IRedisNativeClientAsync.HSetNXAsync(string hashId, byte[] key, byte[] value, CancellationToken token)
        {
            AssertHashIdAndKey(hashId, key);
            return SendExpectLongAsync(token, Commands.HSetNx, hashId.ToUtf8Bytes(), key, value);
        }

        ValueTask<long> IRedisNativeClientAsync.HIncrbyAsync(string hashId, byte[] key, int incrementBy, CancellationToken token)
        {
            AssertHashIdAndKey(hashId, key);
            return SendExpectLongAsync(token, Commands.HIncrBy, hashId.ToUtf8Bytes(), key, incrementBy.ToString().ToUtf8Bytes());
        }

        ValueTask<double> IRedisNativeClientAsync.HIncrbyFloatAsync(string hashId, byte[] key, double incrementBy, CancellationToken token)
        {
            AssertHashIdAndKey(hashId, key);
            return SendExpectDoubleAsync(token, Commands.HIncrByFloat, hashId.ToUtf8Bytes(), key, incrementBy.ToString(CultureInfo.InvariantCulture).ToUtf8Bytes());
        }

        ValueTask<byte[]> IRedisNativeClientAsync.HGetAsync(string hashId, byte[] key, CancellationToken token)
            => HGetAsync(hashId.ToUtf8Bytes(), key, token);

        private ValueTask<byte[]> HGetAsync(byte[] hashId, byte[] key, CancellationToken token)
        {
            AssertHashIdAndKey(hashId, key);
            return SendExpectDataAsync(token, Commands.HGet, hashId, key);
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.HMGetAsync(string hashId, byte[][] keys, CancellationToken token)
        {
            AssertNotNull(hashId, nameof(hashId));
            if (keys.Length == 0)
                throw new ArgumentNullException(nameof(keys));

            var cmdArgs = MergeCommandWithArgs(Commands.HMGet, hashId.ToUtf8Bytes(), keys);
            return SendExpectMultiDataAsync(token, cmdArgs);
        }

        ValueTask<long> IRedisNativeClientAsync.HDelAsync(string hashId, byte[] key, CancellationToken token)
            => HDelAsync(hashId.ToUtf8Bytes(), key, token);

        private ValueTask<long> HDelAsync(byte[] hashId, byte[] key, CancellationToken token)
        {
            AssertHashIdAndKey(hashId, key);
            return SendExpectLongAsync(token, Commands.HDel, hashId, key);
        }

        ValueTask<long> IRedisNativeClientAsync.HExistsAsync(string hashId, byte[] key, CancellationToken token)
        {
            AssertHashIdAndKey(hashId, key);
            return SendExpectLongAsync(token, Commands.HExists, hashId.ToUtf8Bytes(), key);
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.HKeysAsync(string hashId, CancellationToken token)
        {
            AssertNotNull(hashId, nameof(hashId));
            return SendExpectMultiDataAsync(token, Commands.HKeys, hashId.ToUtf8Bytes());
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.HValsAsync(string hashId, CancellationToken token)
        {
            AssertNotNull(hashId, nameof(hashId));
            return SendExpectMultiDataAsync(token, Commands.HVals, hashId.ToUtf8Bytes());
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.HGetAllAsync(string hashId, CancellationToken token)
        {
            AssertNotNull(hashId, nameof(hashId));
            return SendExpectMultiDataAsync(token, Commands.HGetAll, hashId.ToUtf8Bytes());
        }

        ValueTask<long> IRedisNativeClientAsync.GeoAddAsync(string key, double longitude, double latitude, string member, CancellationToken token)
        {
            AssertNotNull(key, nameof(key));
            AssertNotNull(member, nameof(member));
            return SendExpectLongAsync(token, Commands.GeoAdd, key.ToUtf8Bytes(), longitude.ToUtf8Bytes(), latitude.ToUtf8Bytes(), member.ToUtf8Bytes());
        }

        ValueTask<long> IRedisNativeClientAsync.GeoAddAsync(string key, RedisGeo[] geoPoints, CancellationToken token)
        {
            var cmdWithArgs = GeoAddPrepareArgs(key, geoPoints);
            return SendExpectLongAsync(token, cmdWithArgs);
        }

        ValueTask<double> IRedisNativeClientAsync.GeoDistAsync(string key, string fromMember, string toMember, string unit, CancellationToken token)
        {
            AssertNotNull(key, nameof(key));

            return unit == null
                ? SendExpectDoubleAsync(token, Commands.GeoDist, key.ToUtf8Bytes(), fromMember.ToUtf8Bytes(), toMember.ToUtf8Bytes())
                : SendExpectDoubleAsync(token, Commands.GeoDist, key.ToUtf8Bytes(), fromMember.ToUtf8Bytes(), toMember.ToUtf8Bytes(), unit.ToUtf8Bytes());
        }

        async ValueTask<string[]> IRedisNativeClientAsync.GeoHashAsync(string key, string[] members, CancellationToken token)
        {
            AssertNotNull(key, nameof(key));

            var cmdWithArgs = MergeCommandWithArgs(Commands.GeoHash, key.ToUtf8Bytes(), members.Map(x => x.ToUtf8Bytes()).ToArray());
            var result = await SendExpectMultiDataAsync(token, cmdWithArgs).ConfigureAwait(false);
            return result.ToStringArray();
        }

        async ValueTask<List<RedisGeo>> IRedisNativeClientAsync.GeoPosAsync(string key, string[] members, CancellationToken token)
        {
            AssertNotNull(key, nameof(key));

            var cmdWithArgs = MergeCommandWithArgs(Commands.GeoPos, key.ToUtf8Bytes(), members.Map(x => x.ToUtf8Bytes()).ToArray());
            var data = await SendExpectComplexResponseAsync(token, cmdWithArgs).ConfigureAwait(false);
            return GeoPosParseResult(members, data);
        }

        async ValueTask<List<RedisGeoResult>> IRedisNativeClientAsync.GeoRadiusAsync(string key, double longitude, double latitude, double radius, string unit, bool withCoords, bool withDist, bool withHash, int? count, bool? asc, CancellationToken token)
        {
            var cmdWithArgs = GeoRadiusPrepareArgs(key, longitude, latitude, radius, unit,
                withCoords, withDist, withHash, count, asc);

            var to = new List<RedisGeoResult>();

            if (!(withCoords || withDist || withHash))
            {
                var members = (await SendExpectMultiDataAsync(token, cmdWithArgs).ConfigureAwait(false)).ToStringArray();
                foreach (var member in members)
                {
                    to.Add(new RedisGeoResult { Member = member });
                }
            }
            else
            {
                var data = await SendExpectComplexResponseAsync(token, cmdWithArgs).ConfigureAwait(false);
                GetRadiusParseResult(unit, withCoords, withDist, withHash, to, data);
            }

            return to;
        }

        async ValueTask<List<RedisGeoResult>> IRedisNativeClientAsync.GeoRadiusByMemberAsync(string key, string member, double radius, string unit, bool withCoords, bool withDist, bool withHash, int? count, bool? asc, CancellationToken token)
        {
            var cmdWithArgs = GeoRadiusByMemberPrepareArgs(key, member, radius, unit, withCoords, withDist, withHash, count, asc);

            var to = new List<RedisGeoResult>();

            if (!(withCoords || withDist || withHash))
            {
                var members = (await SendExpectMultiDataAsync(token, cmdWithArgs).ConfigureAwait(false)).ToStringArray();
                foreach (var x in members)
                {
                    to.Add(new RedisGeoResult { Member = x });
                }
            }
            else
            {
                var data = await SendExpectComplexResponseAsync(token, cmdWithArgs).ConfigureAwait(false);
                GeoRadiusByMemberParseResult(unit, withCoords, withDist, withHash, to, data);
            }

            return to;
        }

        ValueTask<long> IRedisNativeClientAsync.PublishAsync(string toChannel, byte[] message, CancellationToken token)
            => SendExpectLongAsync(token, Commands.Publish, toChannel.ToUtf8Bytes(), message);

        ValueTask<byte[][]> IRedisNativeClientAsync.SubscribeAsync(string[] toChannels, CancellationToken token)
        {
            if (toChannels.Length == 0)
                throw new ArgumentNullException(nameof(toChannels));

            var cmdWithArgs = MergeCommandWithArgs(Commands.Subscribe, toChannels);
            return SendExpectMultiDataAsync(token, cmdWithArgs);
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.UnSubscribeAsync(string[] fromChannels, CancellationToken token)
        {
            var cmdWithArgs = MergeCommandWithArgs(Commands.UnSubscribe, fromChannels);
            return SendExpectMultiDataAsync(token, cmdWithArgs);
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.PSubscribeAsync(string[] toChannelsMatchingPatterns, CancellationToken token)
        {
            if (toChannelsMatchingPatterns.Length == 0)
                throw new ArgumentNullException(nameof(toChannelsMatchingPatterns));

            var cmdWithArgs = MergeCommandWithArgs(Commands.PSubscribe, toChannelsMatchingPatterns);
            return SendExpectMultiDataAsync(token, cmdWithArgs);
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.PUnSubscribeAsync(string[] fromChannelsMatchingPatterns, CancellationToken token)
        {
            var cmdWithArgs = MergeCommandWithArgs(Commands.PUnSubscribe, fromChannelsMatchingPatterns);
            return SendExpectMultiDataAsync(token, cmdWithArgs);
        }

        ValueTask<byte[][]> IRedisNativeClientAsync.ReceiveMessagesAsync(CancellationToken token)
            => ReadMultiDataAsync(token);

        ValueTask<IRedisSubscriptionAsync> IRedisNativeClientAsync.CreateSubscriptionAsync(CancellationToken token)
            => new RedisSubscription(this).AsValueTaskResult<IRedisSubscriptionAsync>();

        ValueTask<long> IRedisNativeClientAsync.BitCountAsync(string key, CancellationToken token)
        {
            AssertNotNull(key);
            return SendExpectLongAsync(token, Commands.BitCount, key.ToUtf8Bytes());
        }

        ValueTask<long> IRedisNativeClientAsync.DelAsync(params string[] keys)
            => AsAsync().DelAsync(keys, default);

        ValueTask IRedisNativeClientAsync.SInterStoreAsync(string intoSetId, params string[] setIds)
            => AsAsync().SInterStoreAsync(intoSetId, setIds, default);

        ValueTask<byte[][]> IRedisNativeClientAsync.SUnionAsync(params string[] setIds)
            => AsAsync().SUnionAsync(setIds, default);

        ValueTask IRedisNativeClientAsync.WatchAsync(params string[] keys)
            => AsAsync().WatchAsync(keys, default);

        ValueTask<byte[][]> IRedisNativeClientAsync.SubscribeAsync(params string[] toChannels)
            => AsAsync().SubscribeAsync(toChannels, default);

        ValueTask<byte[][]> IRedisNativeClientAsync.UnSubscribeAsync(params string[] toChannels)
            => AsAsync().UnSubscribeAsync(toChannels, default);

        ValueTask<byte[][]> IRedisNativeClientAsync.PSubscribeAsync(params string[] toChannelsMatchingPatterns)
            => AsAsync().PSubscribeAsync(toChannelsMatchingPatterns, default);

        ValueTask<byte[][]> IRedisNativeClientAsync.PUnSubscribeAsync(params string[] toChannelsMatchingPatterns)
            => AsAsync().PUnSubscribeAsync(toChannelsMatchingPatterns, default);

        ValueTask<byte[][]> IRedisNativeClientAsync.SInterAsync(params string[] setIds)
            => AsAsync().SInterAsync(setIds, default);

        ValueTask<byte[][]> IRedisNativeClientAsync.SDiffAsync(string fromSetId, params string[] withSetIds)
            => AsAsync().SDiffAsync(fromSetId, withSetIds, default);

        ValueTask IRedisNativeClientAsync.SDiffStoreAsync(string intoSetId, string fromSetId, params string[] withSetIds)
            => AsAsync().SDiffStoreAsync(intoSetId, fromSetId, withSetIds, default);

        ValueTask<long> IRedisNativeClientAsync.ZUnionStoreAsync(string intoSetId, params string[] setIds)
            => AsAsync().ZUnionStoreAsync(intoSetId, setIds, default);

        ValueTask<long> IRedisNativeClientAsync.ZInterStoreAsync(string intoSetId, params string[] setIds)
            => AsAsync().ZInterStoreAsync(intoSetId, setIds, default);

        ValueTask<RedisData> IRedisNativeClientAsync.EvalCommandAsync(string luaBody, int numberKeysInArgs, params byte[][] keys)
            => AsAsync().EvalCommandAsync(luaBody, numberKeysInArgs, keys, default);

        ValueTask<RedisData> IRedisNativeClientAsync.EvalShaCommandAsync(string sha1, int numberKeysInArgs, params byte[][] keys)
            => AsAsync().EvalShaCommandAsync(sha1, numberKeysInArgs, keys, default);

        ValueTask<byte[][]> IRedisNativeClientAsync.EvalAsync(string luaBody, int numberOfKeys, params byte[][] keysAndArgs)
            => AsAsync().EvalAsync(luaBody, numberOfKeys, keysAndArgs, default);

        ValueTask<byte[][]> IRedisNativeClientAsync.EvalShaAsync(string sha1, int numberOfKeys, params byte[][] keysAndArgs)
            => AsAsync().EvalShaAsync(sha1, numberOfKeys, keysAndArgs, default);

        ValueTask<long> IRedisNativeClientAsync.EvalIntAsync(string luaBody, int numberOfKeys, params byte[][] keysAndArgs)
            => AsAsync().EvalIntAsync(luaBody, numberOfKeys, keysAndArgs, default);

        ValueTask<long> IRedisNativeClientAsync.EvalShaIntAsync(string sha1, int numberOfKeys, params byte[][] keysAndArgs)
            => AsAsync().EvalShaIntAsync(sha1, numberOfKeys, keysAndArgs, default);

        ValueTask<string> IRedisNativeClientAsync.EvalStrAsync(string luaBody, int numberOfKeys, params byte[][] keysAndArgs)
            => AsAsync().EvalStrAsync(luaBody, numberOfKeys, keysAndArgs, default);

        ValueTask<string> IRedisNativeClientAsync.EvalShaStrAsync(string sha1, int numberOfKeys, params byte[][] keysAndArgs)
            => AsAsync().EvalShaStrAsync(sha1, numberOfKeys, keysAndArgs, default);

        ValueTask<RedisData> IRedisNativeClientAsync.RawCommandAsync(params object[] cmdWithArgs)
            => AsAsync().RawCommandAsync(cmdWithArgs, default);

        ValueTask<RedisData> IRedisNativeClientAsync.RawCommandAsync(params byte[][] cmdWithBinaryArgs)
            => AsAsync().RawCommandAsync(cmdWithBinaryArgs, default);

        ValueTask<byte[][]> IRedisNativeClientAsync.MGetAsync(params string[] keys)
            => AsAsync().MGetAsync(keys, default);

        ValueTask<bool> IRedisNativeClientAsync.PfAddAsync(string key, params byte[][] elements)
            => AsAsync().PfAddAsync(key, elements, default);

        ValueTask<byte[][]> IRedisNativeClientAsync.HMGetAsync(string hashId, params byte[][] keysAndArgs)
            => AsAsync().HMGetAsync(hashId, keysAndArgs, default);

        ValueTask<byte[][]> IRedisNativeClientAsync.MGetAsync(params byte[][] keysAndArgs)
            => AsAsync().MGetAsync(keysAndArgs, default);

        ValueTask IRedisNativeClientAsync.SUnionStoreAsync(string intoSetId, params string[] setIds)
            => AsAsync().SUnionStoreAsync(intoSetId, setIds, default);

        ValueTask<byte[][]> IRedisNativeClientAsync.ScriptExistsAsync(params byte[][] sha1Refs)
            => AsAsync().ScriptExistsAsync(sha1Refs, default);

        ValueTask IRedisNativeClientAsync.PfMergeAsync(string toKeyId, params string[] fromKeys)
            => AsAsync().PfMergeAsync(toKeyId, fromKeys, default);

        ValueTask<long> IRedisNativeClientAsync.GeoAddAsync(string key, params RedisGeo[] geoPoints)
            => AsAsync().GeoAddAsync(key, geoPoints, default);

        ValueTask<string[]> IRedisNativeClientAsync.GeoHashAsync(string key, params string[] members)
            => AsAsync().GeoHashAsync(key, members, default);

        ValueTask<List<RedisGeo>> IRedisNativeClientAsync.GeoPosAsync(string key, params string[] members)
            => AsAsync().GeoPosAsync(key, members, default);
    }
}