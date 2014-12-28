// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;
using System.Data;
using ServiceStack.Caching;
using ServiceStack.Data;
using ServiceStack.Messaging;
using ServiceStack.Redis;

namespace ServiceStack
{
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
        public virtual IDbConnection Db
        {
            get { return db ?? (db = DbFactory.OpenDbConnection()); }
        }

        public virtual void Dispose()
        {
            if (db != null)
                db.Dispose();
        }
    }

    public abstract class LogicBase : RepositoryBase, ILogic
    {
        public virtual IRedisClientsManager RedisManager { get; set; }

        private IRedisClient redis;
        public virtual IRedisClient Redis
        {
            get { return redis ?? (redis = RedisManager.GetClient()); }
        }

        private ICacheClient cache;
        public virtual ICacheClient Cache
        {
            get
            {
                return cache ?? (cache = RedisManager != null ? RedisManager.GetCacheClient() : ServiceExtensions.DefaultCache);
            }
            set { cache = value; }
        }

        public virtual IMessageFactory MessageFactory { get; set; }

        private IMessageProducer messageProducer;
        public virtual IMessageProducer MessageProducer
        {
            get { return messageProducer ?? (messageProducer = MessageFactory.CreateMessageProducer()); }
            set { messageProducer = value; }
        }

        public virtual void PublishMessage<T>(T message)
        {
            if (MessageProducer == null)
                throw new NullReferenceException("No IMessageFactory was registered, cannot PublishMessage");

            MessageProducer.Publish(message);
        }
        
        public override void Dispose()
        {
            base.Dispose();

            if (redis != null)
                redis.Dispose();

            if (cache != null)
                cache.Dispose();

            if (messageProducer != null)
                messageProducer.Dispose();
        }
    }
}