//
// https://github.com/ServiceStack/ServiceStack.Redis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2017 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

namespace ServiceStack.Redis
{
    public enum RedisKeyType
    {
        None,
        String,
        List,
        Set,
        SortedSet,
        Hash
    }
}