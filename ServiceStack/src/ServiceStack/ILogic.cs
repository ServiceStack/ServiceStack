// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;
using System.Data;
using ServiceStack.Caching;
using ServiceStack.Data;
using ServiceStack.Messaging;
using ServiceStack.Model;
using ServiceStack.Redis;

namespace ServiceStack;

/// <summary>
/// A convenient repository base class you can inherit from to reduce the boilerplate 
/// with accessing a managed IDbConnection
/// </summary>
public interface IRepository
{
    IDbConnectionFactory DbFactory { get; }
    IDbConnection Db { get; }
}

/// <summary>
/// A convenient base class for your injected service dependencies that reduces the boilerplate
/// with managed access to ServiceStack's built-in providers
/// </summary>
public interface ILogic : IRepository
{
    IRedisClientsManager RedisManager { get; }
    IRedisClient Redis { get; }
    ICacheClient Cache { get; }
    IMessageFactory MessageFactory { get; }
    IMessageProducer MessageProducer { get; }
    void PublishMessage<T>(T message);
}

public abstract class RepositoryBase : IDisposable, IRepository
{
    public virtual IDbConnectionFactory DbFactory { get; set; }

    IDbConnection db;
    public virtual IDbConnection Db => db ??= 
        (DbFactory as IDbConnectionFactoryExtended)?.OpenDbConnection(x => {
            if (x is IHasTag hasTag)
                hasTag.Tag = GetType().Name;
        }) 
        ?? DbFactory.OpenDbConnection();

    public virtual void Dispose() => db?.Dispose();
}

public abstract class LogicBase : RepositoryBase, ILogic
{
    public virtual IRedisClientsManager RedisManager { get; set; }

    private IRedisClient redis;
    public virtual IRedisClient Redis => redis ??= RedisManager.GetClient();

    private ICacheClient cache;
    public virtual ICacheClient Cache
    {
        get => cache ??= RedisManager != null ? RedisManager.GetCacheClient() : ServiceStackHost.DefaultCache;
        set => cache = value;
    }

    public virtual IMessageFactory MessageFactory { get; set; }

    private IMessageProducer messageProducer;
    public virtual IMessageProducer MessageProducer
    {
        get => messageProducer ??= MessageFactory.CreateMessageProducer();
        set => messageProducer = value;
    }

    public virtual void PublishMessage<T>(T message) => HostContext.AppHost.PublishMessage(MessageProducer, message);

    public override void Dispose()
    {
        base.Dispose();

        redis?.Dispose();
        cache?.Dispose();
        messageProducer?.Dispose();
    }
}