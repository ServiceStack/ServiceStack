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

using System;
using System.Collections.Generic;
using ServiceStack.Model;

namespace ServiceStack.Redis.Generic
{
    /// <summary>
    /// Wrap the common redis list operations under a IList[string] interface.
    /// </summary>

    public interface IRedisList<T>
        : IList<T>, IHasStringId
    {
        List<T> GetAll();
        List<T> GetRange(int startingFrom, int endingAt);
        List<T> GetRangeFromSortedList(int startingFrom, int endingAt);
        void RemoveAll();
        void Trim(int keepStartingFrom, int keepEndingAt);
        long RemoveValue(T value);
        long RemoveValue(T value, int noOfMatches);

        void AddRange(IEnumerable<T> values);
        void Append(T value);
        void Prepend(T value);
        T RemoveStart();
        T BlockingRemoveStart(TimeSpan? timeOut);
        T RemoveEnd();

        void Enqueue(T value);
        T Dequeue();
        T BlockingDequeue(TimeSpan? timeOut);

        void Push(T value);
        T Pop();
        T BlockingPop(TimeSpan? timeOut);
        T PopAndPush(IRedisList<T> toList);
    }
}