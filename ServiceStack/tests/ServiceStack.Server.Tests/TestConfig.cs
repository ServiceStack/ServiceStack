// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using ServiceStack.Logging;
using ServiceStack.Redis;
using ServiceStack.Support;

namespace ServiceStack.Server.Tests;

public static class TestConfig
{
    static TestConfig()
    {
        LogManager.LogFactory = new InMemoryLogFactory();
    }

    public const bool IgnoreLongTests = true;

    public const string SingleHost = "localhost";
    public static readonly string[] MasterHosts = new[] { "localhost" };
    public static readonly string[] SlaveHosts = new[] { "localhost" };

    public const int RedisPort = 6379;

    public static string SingleHostConnectionString => SingleHost + ":" + RedisPort;

    public static BasicRedisClientManager BasicClientManger
    {
        get
        {
            return new BasicRedisClientManager(new[] {
                SingleHostConnectionString
            });
        }
    }
}
