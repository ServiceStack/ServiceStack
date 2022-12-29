using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using ServiceStack.Script;

namespace ServiceStack.Redis
{
    public class RedisSearchCursorResult
    {
        public int Cursor { get; set; }
        public List<RedisSearchResult> Results { get; set; }
    }

    public class RedisSearchResult
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public long Ttl { get; set; }
        public long Size { get; set; }
    }

    [Obsolete("Use RedisScripts")]
    public class TemplateRedisFilters : RedisScripts {}
    
    public class RedisScripts : ScriptMethods
    {
        private const string RedisConnection = "__redisConnection";
        
        private IRedisClientsManager redisManager;
        public IRedisClientsManager RedisManager
        {
            get => redisManager ?? (redisManager = Context.Container.Resolve<IRedisClientsManager>());
            set => redisManager = value;
        }

        T exec<T>(Func<IRedisClient, T> fn, ScriptScopeContext scope, object options)
        {
            try
            {
                if ((options is Dictionary<string, object> obj && obj.TryGetValue("connectionString", out var oRedisConn))
                    || scope.PageResult.Args.TryGetValue(RedisConnection, out oRedisConn))
                {
                    using (var redis = new RedisClient((string)oRedisConn))
                    {
                        return fn(redis);
                    }
                }
                
                using (var redis = RedisManager.GetClient())
                {
                    return fn(redis);
                }
            }
            catch (Exception ex)
            {
                throw new StopFilterExecutionException(scope, options, ex);
            }
        }

        public IgnoreResult useRedis(ScriptScopeContext scope, string redisConnection)
        {
            if (redisConnection == null)
                scope.PageResult.Args.Remove(RedisConnection);
            else
                scope.PageResult.Args[RedisConnection] = redisConnection;

            return IgnoreResult.Value;
        }

        static readonly Dictionary<string, int> cmdArgCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) {
            { "SET", 3 }
        };

        List<string> parseCommandString(string cmd)
        {
            var args = new List<string>();
            var lastPos = 0;
            for (var i = 0; i < cmd.Length; i++)
            {
                var c = cmd[i];
                if (c == '{' || c == '[')
                {
                    break; //stop splitting args if value is complex type
                }
                if (c == ' ')
                {
                    var arg = cmd.Substring(lastPos, i - lastPos);
                    args.Add(arg);
                    lastPos = i + 1;

                    //if we've reached the command args count, capture the rest of the body as the last arg
                    if (cmdArgCounts.TryGetValue(args[0], out int argCount) && args.Count == argCount - 1)
                        break;
                }
            }
            args.Add(cmd.Substring(lastPos));
            return args;
        }

        object toObject(RedisText r)
        {
            if (r == null)
                return null;

            if (r.Children != null && r.Children.Count > 0)
            {
                var to = new List<object>();
                for (var i = 0; i < r.Children.Count; i++)
                {
                    var child = r.Children[i];
                    var value = child.Text ?? toObject(child);
                    to.Add(value);
                }
                return to;
            }
            return r.Text;
        }

        public object redisCall(ScriptScopeContext scope, object redisCommand) => redisCall(scope, redisCommand, null);
        public object redisCall(ScriptScopeContext scope, object redisCommand, object options)
        {
            if (redisCommand == null)
                return null;

            List<string> args;
            if (redisCommand is string cmd)
            {
                if (string.IsNullOrEmpty(cmd))
                    return null;

                args = parseCommandString(cmd);
            }
            else if (redisCommand is IEnumerable e && !(e is IDictionary))
            {
                args = new List<string>();
                foreach (var arg in e)
                {
                    if (arg == null) continue;
                    args.Add(arg.ToString());
                }
            }
            else
                throw new NotSupportedException($"redisCall expects a string or an object args but received a {redisCommand.GetType().Name} instead.");

            var objParams = args.Select(x => (object)x).ToArray();
            var redisText = exec(r => r.Custom(objParams), scope, options);
            var result = toObject(redisText);
            return result;
        }

        public List<RedisSearchResult> redisSearchKeys(ScriptScopeContext scope, string query) => redisSearchKeys(scope, query, null);
        public List<RedisSearchResult> redisSearchKeys(ScriptScopeContext scope, string query, object options)
        {
            var json = redisSearchKeysAsJson(scope, query, options);
            const string noResult = "{\"cursor\":0,\"results\":{}}";
            if (json == noResult)
                return new List<RedisSearchResult>();

            var searchResults = json.FromJson<RedisSearchCursorResult>();
            return searchResults.Results;
        }

        public Dictionary<string, string> redisInfo(ScriptScopeContext scope) => redisInfo(scope, null);
        public Dictionary<string, string> redisInfo(ScriptScopeContext scope, object options) => exec(r => r.Info, scope, options);

        public string redisConnectionString(ScriptScopeContext scope) => exec(r => $"{r.Host}:{r.Port}?db={r.Db}", scope, null);

        public Dictionary<string, object> redisConnection(ScriptScopeContext scope) => exec(r => new Dictionary<string, object>
        {
            { "host", r.Host },
            { "port", r.Port },
            { "db", r.Db },
        }, scope, null);

        public string redisToConnectionString(ScriptScopeContext scope, object connectionInfo) => redisToConnectionString(scope, connectionInfo, null);
        public string redisToConnectionString(ScriptScopeContext scope, object connectionInfo, object options)
        {
            var connectionString = connectionInfo as string;
            if (connectionString != null)
                return connectionString;

            if (connectionInfo is IDictionary<string, object> d)
            {
                var host = (d.TryGetValue("host", out object h) ? h as string : null) ?? "localhost";
                var port = d.TryGetValue("port", out object p) ? DynamicInt.Instance.ConvertFrom(p) : 6379;
                var db = d.TryGetValue("db", out object oDb) ? DynamicInt.Instance.ConvertFrom(oDb) : 0;

                connectionString = $"{host}:{port}?db={db}";

                if (d.TryGetValue("password", out object password))
                    connectionString += "&password=" + password.ToString().UrlEncode();
            }

            return connectionString;
        }

        public string redisChangeConnection(ScriptScopeContext scope, object newConnection) => redisChangeConnection(scope, newConnection, null);
        public string redisChangeConnection(ScriptScopeContext scope, object newConnection, object options)
        {
            try
            {
                var connectionString = redisToConnectionString(scope, newConnection, options);
                if (connectionString == null)
                    throw new NotSupportedException(nameof(redisChangeConnection) + " expects a String or an ObjectDictionary but received: " + (newConnection?.GetType().Name ?? "null"));

                using (var testConnection = new RedisClient(connectionString))
                {
                    testConnection.Ping();
                }

                ((IRedisFailover)RedisManager).FailoverTo(connectionString);

                return connectionString;
            }
            catch (Exception ex)
            {
                throw new StopFilterExecutionException(scope, options ?? newConnection as IDictionary<string,object>, ex);
            }
        }

        public string redisSearchKeysAsJson(ScriptScopeContext scope, string query, object options)
        {
            if (string.IsNullOrEmpty(query))
                return null;

            try
            {
                var args = scope.AssertOptions(nameof(redisSearchKeys), options);
                var limit = args.TryGetValue("limit", out object value)
                    ? value.ConvertTo<int>()
                    : scope.GetValue("redis.search.limit") ?? 100;

                const string LuaScript = @"
local limit = tonumber(ARGV[2])
local pattern = ARGV[1]
local cursor = tonumber(ARGV[3])
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
local cursorAttrs = {['cursor'] = cursor, ['results'] = {}}
if len == 0 then
    return cjson.encode(cursorAttrs)
end

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
    local attrs = {['id'] = key, ['type'] = type, ['ttl'] = pttl, ['size'] = size, ['foo'] = 'bar'}
    table.insert(keyAttrs, attrs)    
end
cursorAttrs['results'] = keyAttrs
return cjson.encode(cursorAttrs)";

                var json = exec(r => r.ExecCachedLua(LuaScript, sha1 =>
                    r.ExecLuaShaAsString(sha1, query, limit.ToString(), "0")), scope, options);

                return json;
            }
            catch (Exception ex)
            {
                throw new StopFilterExecutionException(scope, options, ex);
            }
        }
    }
}
