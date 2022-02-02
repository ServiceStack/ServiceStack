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

namespace ServiceStack.Redis
{
    public partial class RedisClient : IRedisClient
    {
        internal partial class RedisClientSortedSets
            : IHasNamed<IRedisSortedSetAsync>
        {
            IRedisSortedSetAsync IHasNamed<IRedisSortedSetAsync>.this[string setId]
            {
                get => new RedisClientSortedSet(client, setId);
                set => throw new NotSupportedException();
            }
        }
    }
}