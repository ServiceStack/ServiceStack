//
// https://github.com/ServiceStack/ServiceStack.Redis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

namespace ServiceStack.Redis
{
    public class RedisClientManagerConfig
    {
        public RedisClientManagerConfig()
        {
            AutoStart = true; //Simplifies the most common use-case - registering in an IOC
        }

        public long? DefaultDb { get; set; }
        public int MaxReadPoolSize { get; set; }
        public int MaxWritePoolSize { get; set; }
        public bool AutoStart { get; set; }
    }
}