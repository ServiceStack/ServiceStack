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
       internal partial class RedisClientSets
            : IHasNamed<IRedisSetAsync>
        {
            IRedisSetAsync IHasNamed<IRedisSetAsync>.this[string setId]
            {
                get => new RedisClientSet(client, setId);
                set => throw new NotSupportedException();
            }
        }
    }
}
