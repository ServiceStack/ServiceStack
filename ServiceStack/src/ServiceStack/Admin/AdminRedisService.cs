#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.DataAnnotations;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace ServiceStack.Admin;

[ExcludeMetadata, Tag(TagNames.Admin)]
public class AdminRedis : IPost, IReturn<AdminRedisResponse>
{
    public int? Db { get; set; }
    public string? Query { get; set; }
    public RedisEndpointInfo? Reconnect { get; set; }
    public int? Take { get; set; }
    public int? Position { get; set; }
    public List<string>? Args { get; set; }
}
public class AdminRedisResponse : IHasResponseStatus
{
    public long Db { get; set; }
    public List<RedisSearchResult>? SearchResults { get; set; }
    public Dictionary<string, string>? Info { get; set; }
    public RedisEndpointInfo? Endpoint { get; set; }
    public RedisText? Result { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

public class RedisSearchResult
{
    public string Id { get; set; }
    public string Type { get; set; }
    public long Ttl { get; set; }
    public long Size { get; set; }
}

public class AdminRedisService : Service
{
    public class SearchCursorResult
    {
        public int Cursor { get; set; }
        public List<RedisSearchResult> Results { get; set; }
    }
    
    private async Task<AdminRedisFeature> AssertRequiredRole()
    {
        var feature = AssertPlugin<AdminRedisFeature>();
        await RequiredRoleAttribute.AssertRequiredRoleAsync(Request, feature.AdminRole);
        return feature;
    }

    public async Task<object> Post(AdminRedis request)
    {
        var feature = await AssertRequiredRole();

        if (request.Reconnect != null)
        {
            if (feature.ModifiableConnection != true)
                throw new Exception("Modifying Redis Connection is not permitted, to allow set ModifiableConnection=true");

            ChangeConnection(request.Reconnect);
            return new AdminRedisResponse {
                Endpoint = ToRedisEndpointInfo(TryResolve<IRedisClientsManager>()?.RedisResolver.PrimaryEndpoint)
            };
        }
        
        var redis = await GetRedisAsync();

        if (request.Db != null && request.Db != redis.Db)
            await redis.SelectAsync(request.Db.Value); 
        
        var to = new AdminRedisResponse {
            Db = redis.Db,
        };

        if (request.Query != null)
            to.SearchResults = (await Search(redis, request.Query!, request.Position ?? 0, request.Take ?? feature.QueryLimit)).Results;

        if (request.Args?.Count > 0)
        {
            var firstArg = request.Args[0];
            if (!string.IsNullOrEmpty(firstArg))
            {
                if (feature.IllegalCommands.Contains(firstArg))
                    throw new ArgumentException("Command is not allowed");

                if (firstArg == "INFO")
                {
                    to.Info = await redis.InfoAsync();
                    to.Endpoint = ToRedisEndpointInfo(TryResolve<IRedisClientsManager>()?.RedisResolver.PrimaryEndpoint);
                }
                else
                {
                    to.Result = await redis.CustomAsync(request.Args.ToArray());
                }
            }
        }
        
        return to;
    }

    RedisEndpointInfo? ToRedisEndpointInfo(IRedisEndpoint? endpoint) => endpoint == null
        ? null
        : new() {
            Host = endpoint.Host,
            Port = endpoint.Port,
            Ssl = endpoint.Ssl ? true : null,
            Db = endpoint.Db,
            Username = endpoint.Username,
            Password = endpoint.Password,
        };

    private void ChangeConnection(RedisEndpointInfo endpoint)
    {
        var connectionString = ToConnectionString(endpoint);
        var redisManager = TryResolve<IRedisClientsManager>();

        var testClient = redisManager.RedisResolver.CreateClient(connectionString);
        try
        {
            var role = testClient.GetServerRole();
            if (role != RedisServerRole.Master)
                throw new InfoException("Not a Master Server");
        }
        catch (Exception e)
        {
            throw new InfoException($"Could not connect to Redis master server: {e.Message}");
        }

        var appSettingsPath = HostContext.AppHost.AppSettingsPath;
        if (appSettingsPath != null)
            AppSettingsUtils.SaveAppSetting(appSettingsPath, "redis.connection", connectionString);
        
        redisManager.RedisResolver.ResetMasters(new[] { connectionString });
    }

    public static string ToConnectionString(RedisEndpointInfo endpoint)
    {
        var sb = StringBuilderCache.Allocate();
        sb.Append(endpoint.Host).Append(':').Append(endpoint.Port);

        var options = new List<string>();
        if (endpoint.Db != 0)
            options.Add("db=" + endpoint.Db);
        if (endpoint.Ssl == true)
            options.Add("ssl=true");
        if (!string.IsNullOrEmpty(endpoint.Username))
            options.Add("username=" + endpoint.Username);
        if (!string.IsNullOrEmpty(endpoint.Password))
            options.Add("password=" + endpoint.Password.UrlEncode());
        
        if (options.Count > 0)
            sb.Append('?').Append(options.Join("&"));
        
        return StringBuilderCache.ReturnAndFree(sb);
    }

    public async Task<SearchCursorResult> Search(IRedisClientAsync redis, string query, int position, int limit)
    {
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
    return cursorAttrs
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
    local attrs = {['id'] = key, ['type'] = type, ['ttl'] = pttl, ['size'] = size}
    table.insert(keyAttrs, attrs)    
end
cursorAttrs['results'] = keyAttrs
return cjson.encode(cursorAttrs)";

        // if (string.IsNullOrEmpty(query))
        //     query = "*";

        var json = await redis.ExecCachedLuaAsync(LuaScript, sha1 =>
            redis.ExecLuaShaAsStringAsync(sha1, query, limit.ToString(), position.ToString()));

        if (string.IsNullOrEmpty(json))
            return new SearchCursorResult { Cursor = 0, Results = new List<RedisSearchResult>() };

        var searchResults = json.FromJson<SearchCursorResult>();
        return searchResults;
    }
}