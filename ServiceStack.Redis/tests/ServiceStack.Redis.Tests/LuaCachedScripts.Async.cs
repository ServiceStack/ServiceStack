using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    [Category("Async")]
    public class LuaCachedScriptsAsync
    {
        private const string LuaScript = @"
local limit = tonumber(ARGV[2])
local pattern = ARGV[1]
local cursor = 0
local len = 0
local results = {}

repeat
    local r = redis.call('scan', cursor, 'MATCH', pattern, 'COUNT', limit)
    cursor = tonumber(r[1])
    for k,v in ipairs(r[2]) do
        table.insert(results, v)
        len = len + 1
        if len == limit then break end
    end
until cursor == 0 or len == limit

return results
";

        private static async Task AddTestKeysAsync(IRedisClientAsync redis, int count)
        {
            for (int i = 0; i < count; i++)
                await redis.SetValueAsync("key:" + i, "value:" + i);
        }

        [Test]
        public async Task Can_call_repeated_scans_in_LUA()
        {
            await using var redis = new RedisClient().ForAsyncOnly();
            await AddTestKeysAsync(redis, 20);

            var r = await redis.ExecLuaAsync(LuaScript, "key:*", "10");
            Assert.That(r.Children.Count, Is.EqualTo(10));

            r = await redis.ExecLuaAsync(LuaScript, "key:*", "40");
            Assert.That(r.Children.Count, Is.EqualTo(20));
        }

        [Test]
        public async Task Can_call_Cached_Lua()
        {
            await using var redis = new RedisClient().ForAsyncOnly();
            await AddTestKeysAsync(redis, 20);

            var r = await redis.ExecCachedLuaAsync(LuaScript, sha1 =>
                redis.ExecLuaShaAsync(sha1, "key:*", "10"));
            Assert.That(r.Children.Count, Is.EqualTo(10));

            r = await redis.ExecCachedLuaAsync(LuaScript, sha1 =>
                redis.ExecLuaShaAsync(sha1, "key:*", "10"));
            Assert.That(r.Children.Count, Is.EqualTo(10));
        }

        [Test]
        public async Task Can_call_Cached_Lua_even_after_script_is_flushed()
        {
            await using var redis = new RedisClient().ForAsyncOnly();
        await AddTestKeysAsync(redis, 20);

            var r = await redis.ExecCachedLuaAsync(LuaScript, sha1 =>
                redis.ExecLuaShaAsync(sha1, "key:*", "10"));
            Assert.That(r.Children.Count, Is.EqualTo(10));

            await ((IRedisNativeClientAsync)redis).ScriptFlushAsync();

            r = await redis.ExecCachedLuaAsync(LuaScript, sha1 =>
                redis.ExecLuaShaAsync(sha1, "key:*", "10"));
            Assert.That(r.Children.Count, Is.EqualTo(10));
        }

        [Test]
        public async Task Can_call_repeated_scans_in_LUA_longhand()
        {
            await using var redis = new RedisClient().ForAsyncOnly();
            
            await AddTestKeysAsync(redis, 20);

            var r = await redis.ExecLuaAsync(LuaScript, null, new[] { "key:*", "10" });
            Assert.That(r.Children.Count, Is.EqualTo(10));

            r = await redis.ExecLuaAsync(LuaScript, null, new[] { "key:*", "40" });
            Assert.That(r.Children.Count, Is.EqualTo(20));
        }

        [Test]
        public async Task Can_call_Cached_Lua_longhand()
        {
            await using var redis = new RedisClient().ForAsyncOnly();
            await AddTestKeysAsync(redis, 20);

            var r = await redis.ExecCachedLuaAsync(LuaScript, sha1 =>
                redis.ExecLuaShaAsync(sha1, null, new[] { "key:*", "10" }));
            Assert.That(r.Children.Count, Is.EqualTo(10));

            r = await redis.ExecCachedLuaAsync(LuaScript, sha1 =>
                redis.ExecLuaShaAsync(sha1, null, new[] { "key:*", "10" }));
            Assert.That(r.Children.Count, Is.EqualTo(10));
        }

        [Test]
        public async Task Can_call_Cached_Lua_even_after_script_is_flushed_longhand()
        {
            await using var redis = new RedisClient().ForAsyncOnly();
            await AddTestKeysAsync(redis, 20);

            var r = await redis.ExecCachedLuaAsync(LuaScript, sha1 =>
                redis.ExecLuaShaAsync(sha1, null, new[] { "key:*", "10" }));
            Assert.That(r.Children.Count, Is.EqualTo(10));

            await ((IRedisNativeClientAsync)redis).ScriptFlushAsync();

            r = await redis.ExecCachedLuaAsync(LuaScript, sha1 =>
                redis.ExecLuaShaAsync(sha1, null, new[] { "key:*", "10" }));
            Assert.That(r.Children.Count, Is.EqualTo(10));
        }

        private const string KeyAttributesScript = @"
local limit = tonumber(ARGV[2])
local pattern = ARGV[1]
local cursor = 0
local len = 0
local keys = {}

repeat
    local r = redis.call('scan', cursor, 'MATCH', pattern, 'COUNT', limit)
    cursor = tonumber(r[1])
    for k,v in ipairs(r[2]) do
        table.insert(keys, v)
        len = len + 1
        if len == limit then break end
    end
until cursor == 0 or len == limit

local keyAttrs = {}
for i,key in ipairs(keys) do
    local type = redis.call('type', key)['ok']
    local pttl = redis.call('pttl', key)
    local size = 0
    if type == 'string' then
        size = redis.call('strlen', key)
    elseif type == 'list' then
        size = redis.call('llen', key)
    elseif type == 'set' then
        size = redis.call('scard', key)
    elseif type == 'zset' then
        size = redis.call('zcard', key)
    elseif type == 'hash' then
        size = redis.call('hlen', key)
    end

    local attrs = {['id'] = key, ['type'] = type, ['ttl'] = pttl, ['size'] = size}

    table.insert(keyAttrs, attrs)
end

return cjson.encode(keyAttrs)";

        [Test]
        public async Task Can_call_script_with_complex_response()
        {
            await using var redis = new RedisClient().ForAsyncOnly();
            var r = await redis.ExecCachedLuaAsync(KeyAttributesScript, sha1 =>
            redis.ExecLuaShaAsStringAsync(sha1, "key:*", "10"));

            r.Print();

            var results = r.FromJson<List<SearchResult>>();

            Assert.That(results.Count, Is.EqualTo(10));

            var result = results[0];
            Assert.That(result.Id.StartsWith("key:"));
            Assert.That(result.Type, Is.EqualTo("string"));
            Assert.That(result.Size, Is.GreaterThan("value:".Length));
            Assert.That(result.Ttl, Is.EqualTo(-1));
        }

        [Test]
        public async Task Can_call_script_with_complex_response_longhand()
        {
            await using var redis = new RedisClient().ForAsyncOnly();
            var r = await redis.ExecCachedLuaAsync(KeyAttributesScript, sha1 =>
            redis.ExecLuaShaAsStringAsync(sha1, null, new[] { "key:*", "10" }));

            r.Print();

            var results = r.FromJson<List<SearchResult>>();

            Assert.That(results.Count, Is.EqualTo(10));

            var result = results[0];
            Assert.That(result.Id.StartsWith("key:"));
            Assert.That(result.Type, Is.EqualTo("string"));
            Assert.That(result.Size, Is.GreaterThan("value:".Length));
            Assert.That(result.Ttl, Is.EqualTo(-1));
        }

        public class SearchResult
        {
            public string Id { get; set; }
            public string Type { get; set; }
            public long Ttl { get; set; }
            public long Size { get; set; }
        }

        [Test]
        public async Task Can_merge_multiple_SearchResults()
        {
            await using var Redis = new RedisClient().ForAsyncOnly();
            var limit = 10;
            var query = "key:*";

            List<string> keys = new List<string>(limit);
            await foreach (var key in Redis.ScanAllKeysAsync(pattern: query, pageSize: limit))
            {
                keys.Add(key);
                if (keys.Count == limit) break;
            }

            var keyTypes = new Dictionary<string, string>();
            var keyTtls = new Dictionary<string, long>();
            var keySizes = new Dictionary<string, long>();

            if (keys.Count > 0)
            {
                await using (var pipeline = Redis.CreatePipeline())
                {
                    foreach (var key in keys)
                        pipeline.QueueCommand(r => r.TypeAsync(key), x => keyTypes[key] = x);

                    foreach (var key in keys)
                        pipeline.QueueCommand(r => ((IRedisNativeClientAsync)r).PTtlAsync(key), x => keyTtls[key] = x);

                    await pipeline.FlushAsync();
                }

                await using (var pipeline = Redis.CreatePipeline())
                {
                    foreach (var entry in keyTypes)
                    {
                        var key = entry.Key;
                        switch (entry.Value)
                        {
                            case "string":
                                pipeline.QueueCommand(r => r.GetStringCountAsync(key), x => keySizes[key] = x);
                                break;
                            case "list":
                                pipeline.QueueCommand(r => r.GetListCountAsync(key), x => keySizes[key] = x);
                                break;
                            case "set":
                                pipeline.QueueCommand(r => r.GetSetCountAsync(key), x => keySizes[key] = x);
                                break;
                            case "zset":
                                pipeline.QueueCommand(r => r.GetSortedSetCountAsync(key), x => keySizes[key] = x);
                                break;
                            case "hash":
                                pipeline.QueueCommand(r => r.GetHashCountAsync(key), x => keySizes[key] = x);
                                break;
                        }
                    }

                    await pipeline.FlushAsync();
                }
            }

            var results = keys.Map(x => new SearchResult
            {
                Id = x,
                Type = keyTypes.GetValueOrDefault(x),
                Ttl = keyTtls.GetValueOrDefault(x),
                Size = keySizes.GetValueOrDefault(x),
            });

            Assert.That(results.Count, Is.EqualTo(limit));

            var result = results[0];
            Assert.That(result.Id.StartsWith("key:"));
            Assert.That(result.Type, Is.EqualTo("string"));
            Assert.That(result.Size, Is.GreaterThan("value:".Length));
            Assert.That(result.Ttl, Is.EqualTo(-1));
        }
    }
}