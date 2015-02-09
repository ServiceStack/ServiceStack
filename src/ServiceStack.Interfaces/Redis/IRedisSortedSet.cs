//
// https://github.com/ServiceStack/ServiceStack.Redis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2014 Service Stack LLC. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System.Collections.Generic;
using ServiceStack.Model;

namespace ServiceStack.Redis
{
    public interface IRedisSortedSet
        : ICollection<string>, IHasStringId
    {
        List<string> GetAll();
        List<string> GetRange(int startingRank, int endingRank);
        List<string> GetRangeByScore(string fromStringScore, string toStringScore);
        List<string> GetRangeByScore(string fromStringScore, string toStringScore, int? skip, int? take);
        List<string> GetRangeByScore(double fromScore, double toScore);
        List<string> GetRangeByScore(double fromScore, double toScore, int? skip, int? take);
        void RemoveRange(int fromRank, int toRank);
        void RemoveRangeByScore(double fromScore, double toScore);
        void StoreFromIntersect(params IRedisSortedSet[] ofSets);
        void StoreFromUnion(params IRedisSortedSet[] ofSets);
        long GetItemIndex(string value);
        double GetItemScore(string value);
        void IncrementItemScore(string value, double incrementByScore);
        string PopItemWithHighestScore();
        string PopItemWithLowestScore();
    }
}