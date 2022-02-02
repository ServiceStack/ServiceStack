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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Generic
{
    public partial class RedisTypedClient<T>
    {
        internal partial class RedisClientLists
            : IHasNamed<IRedisListAsync<T>>
        {
            IRedisListAsync<T> IHasNamed<IRedisListAsync<T>>.this[string listId]
            {
                get => new RedisClientList<T>(client, listId);
                set => throw new NotSupportedException();
            }
        }
    }
}