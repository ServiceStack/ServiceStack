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
    public partial class RedisClient
    {
        internal partial class RedisClientLists
            : IHasNamed<IRedisListAsync>
        {
            IRedisListAsync IHasNamed<IRedisListAsync>.this[string listId]
            {
                get => new RedisClientList(client, listId);
                set => throw new NotSupportedException();
            }
        }
    }
}