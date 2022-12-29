using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class LuaCachedScripts
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

        private static void AddTestKeys(RedisClient redis, int count)
        {
            count.Times(i =>
                redis.SetValue("key:" + i, "value:" + i));
        }

        [Test]
        public void Can_call_repeated_scans_in_LUA()
        {
            using (var redis = new RedisClient())
            {
                AddTestKeys(redis, 20);

                var r = redis.ExecLua(LuaScript, "key:*", "10");
                Assert.That(r.Children.Count, Is.EqualTo(10));

                r = redis.ExecLua(LuaScript, "key:*", "40");
                Assert.That(r.Children.Count, Is.EqualTo(20));
            }
        }

        [Test]
        public void Can_call_Cached_Lua()
        {
            using (var redis = new RedisClient())
            {
                AddTestKeys(redis, 20);

                var r = redis.ExecCachedLua(LuaScript, sha1 =>
                    redis.ExecLuaSha(sha1, "key:*", "10"));
                Assert.That(r.Children.Count, Is.EqualTo(10));

                r = redis.ExecCachedLua(LuaScript, sha1 =>
                    redis.ExecLuaSha(sha1, "key:*", "10"));
                Assert.That(r.Children.Count, Is.EqualTo(10));
            }
        }

        [Test]
        public void Can_call_Cached_Lua_even_after_script_is_flushed()
        {
            using (var redis = new RedisClient())
            {
                AddTestKeys(redis, 20);

                var r = redis.ExecCachedLua(LuaScript, sha1 =>
                    redis.ExecLuaSha(sha1, "key:*", "10"));
                Assert.That(r.Children.Count, Is.EqualTo(10));

                redis.ScriptFlush();

                r = redis.ExecCachedLua(LuaScript, sha1 =>
                    redis.ExecLuaSha(sha1, "key:*", "10"));
                Assert.That(r.Children.Count, Is.EqualTo(10));
            }
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
        public void Can_call_script_with_complex_response()
        {
            using (var redis = new RedisClient())
            {
                var r = redis.ExecCachedLua(KeyAttributesScript, sha1 =>
                    redis.ExecLuaShaAsString(sha1, "key:*", "10"));

                r.Print();

                var results = r.FromJson<List<SearchResult>>();

                Assert.That(results.Count, Is.EqualTo(10));

                var result = results[0];
                Assert.That(result.Id.StartsWith("key:"));
                Assert.That(result.Type, Is.EqualTo("string"));
                Assert.That(result.Size, Is.GreaterThan("value:".Length));
                Assert.That(result.Ttl, Is.EqualTo(-1));
            }
        }

        public class SearchResult
        {
            public string Id { get; set; }
            public string Type { get; set; }
            public long Ttl { get; set; }
            public long Size { get; set; }
        }

        [Test]
        public void Can_merge_multiple_SearchResults()
        {
            var Redis = new RedisClient();
            var limit = 10;
            var query = "key:*";

            var keys = Redis.ScanAllKeys(pattern: query, pageSize: limit)
                .Take(limit).ToList();

            var keyTypes = new Dictionary<string, string>();
            var keyTtls = new Dictionary<string, long>();
            var keySizes = new Dictionary<string, long>();

            if (keys.Count > 0)
            {
                using (var pipeline = Redis.CreatePipeline())
                {
                    keys.Each(key =>
                        pipeline.QueueCommand(r => r.Type(key), x => keyTypes[key] = x));

                    keys.Each(key =>
                        pipeline.QueueCommand(r => ((RedisNativeClient)r).PTtl(key), x => keyTtls[key] = x));

                    pipeline.Flush();
                }

                using (var pipeline = Redis.CreatePipeline())
                {
                    foreach (var entry in keyTypes)
                    {
                        var key = entry.Key;
                        switch (entry.Value)
                        {
                            case "string":
                                pipeline.QueueCommand(r => r.GetStringCount(key), x => keySizes[key] = x);
                                break;
                            case "list":
                                pipeline.QueueCommand(r => r.GetListCount(key), x => keySizes[key] = x);
                                break;
                            case "set":
                                pipeline.QueueCommand(r => r.GetSetCount(key), x => keySizes[key] = x);
                                break;
                            case "zset":
                                pipeline.QueueCommand(r => r.GetSortedSetCount(key), x => keySizes[key] = x);
                                break;
                            case "hash":
                                pipeline.QueueCommand(r => r.GetHashCount(key), x => keySizes[key] = x);
                                break;
                        }
                    }

                    pipeline.Flush();
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