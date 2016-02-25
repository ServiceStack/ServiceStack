//
// https://github.com/ServiceStack/ServiceStack.Redis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2016 Service Stack LLC. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System.Collections.Generic;
using ServiceStack.Model;

namespace ServiceStack.Redis.Generic
{
    public interface IRedisSet<T> : ICollection<T>, IHasStringId
    {
        List<T> Sort(int startingFrom, int endingAt);
        HashSet<T> GetAll();
        T PopRandomItem();
        T GetRandomItem();
        void MoveTo(T item, IRedisSet<T> toSet);
        void PopulateWithIntersectOf(params IRedisSet<T>[] sets);
        void PopulateWithUnionOf(params IRedisSet<T>[] sets);
        void GetDifferences(params IRedisSet<T>[] withSets);
        void PopulateWithDifferencesOf(IRedisSet<T> fromSet, params IRedisSet<T>[] withSets);
    }

}
