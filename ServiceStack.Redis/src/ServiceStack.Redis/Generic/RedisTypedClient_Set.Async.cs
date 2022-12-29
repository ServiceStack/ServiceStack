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

using ServiceStack.Model;
using System;

namespace ServiceStack.Redis.Generic
{
    public partial class RedisTypedClient<T>
    {
        internal partial class RedisClientSets
            : IHasNamed<IRedisSetAsync<T>>
        {
            IRedisSetAsync<T> IHasNamed<IRedisSetAsync<T>>.this[string setId]
            {
                get => new RedisClientSet<T>(client, setId);
                set => throw new NotSupportedException();
            }
        }
    }
}