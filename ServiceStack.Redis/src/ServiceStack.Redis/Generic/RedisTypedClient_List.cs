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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ServiceStack.Model;

namespace ServiceStack.Redis.Generic
{
    public partial class RedisTypedClient<T>
    {
        const int FirstElement = 0;
        const int LastElement = -1;

        public IHasNamed<IRedisList<T>> Lists { get; set; }

        internal partial class RedisClientLists
            : IHasNamed<IRedisList<T>>
        {
            private readonly RedisTypedClient<T> client;

            public RedisClientLists(RedisTypedClient<T> client)
            {
                this.client = client;
            }

            public IRedisList<T> this[string listId]
            {
                get
                {
                    return new RedisClientList<T>(client, listId);
                }
                set
                {
                    var list = this[listId];
                    list.Clear();
                    list.CopyTo(value.ToArray(), 0);
                }
            }
        }

        private List<T> CreateList(byte[][] multiDataList)
        {
            if (multiDataList == null) return new List<T>();

            var results = new List<T>();
            foreach (var multiData in multiDataList)
            {
                results.Add(DeserializeValue(multiData));
            }
            return results;
        }

        public List<T> GetAllItemsFromList(IRedisList<T> fromList)
        {
            var multiDataList = client.LRange(fromList.Id, FirstElement, LastElement);
            return CreateList(multiDataList);
        }

        public List<T> GetRangeFromList(IRedisList<T> fromList, int startingFrom, int endingAt)
        {
            var multiDataList = client.LRange(fromList.Id, startingFrom, endingAt);
            return CreateList(multiDataList);
        }

        public List<T> SortList(IRedisList<T> fromList, int startingFrom, int endingAt)
        {
            var sortOptions = new SortOptions { Skip = startingFrom, Take = endingAt, };
            var multiDataList = client.Sort(fromList.Id, sortOptions);
            return CreateList(multiDataList);
        }

        public void AddItemToList(IRedisList<T> fromList, T value)
        {
            client.RPush(fromList.Id, SerializeValue(value));
        }

        //TODO: replace it with a pipeline implementation ala AddRangeToSet
        public void AddRangeToList(IRedisList<T> fromList, IEnumerable<T> values)
        {
            foreach (var value in values)
            {
                AddItemToList(fromList, value);
            }
        }

        public void PrependItemToList(IRedisList<T> fromList, T value)
        {
            client.LPush(fromList.Id, SerializeValue(value));
        }

        public T RemoveStartFromList(IRedisList<T> fromList)
        {
            return DeserializeValue(client.LPop(fromList.Id));
        }

        public T BlockingRemoveStartFromList(IRedisList<T> fromList, TimeSpan? timeOut)
        {
            var unblockingKeyAndValue = client.BLPop(fromList.Id, (int)timeOut.GetValueOrDefault().TotalSeconds);
            return unblockingKeyAndValue.Length == 0
                ? default(T)
                : DeserializeValue(unblockingKeyAndValue[1]);
        }

        public T RemoveEndFromList(IRedisList<T> fromList)
        {
            return DeserializeValue(client.RPop(fromList.Id));
        }

        public void RemoveAllFromList(IRedisList<T> fromList)
        {
            client.LTrim(fromList.Id, int.MaxValue, FirstElement);
        }

        public void TrimList(IRedisList<T> fromList, int keepStartingFrom, int keepEndingAt)
        {
            client.LTrim(fromList.Id, keepStartingFrom, keepEndingAt);
        }

        public long RemoveItemFromList(IRedisList<T> fromList, T value)
        {
            const int removeAll = 0;
            return client.LRem(fromList.Id, removeAll, SerializeValue(value));
        }

        public long RemoveItemFromList(IRedisList<T> fromList, T value, int noOfMatches)
        {
            return client.LRem(fromList.Id, noOfMatches, SerializeValue(value));
        }

        public long GetListCount(IRedisList<T> fromList)
        {
            return client.LLen(fromList.Id);
        }

        public T GetItemFromList(IRedisList<T> fromList, int listIndex)
        {
            return DeserializeValue(client.LIndex(fromList.Id, listIndex));
        }

        public void SetItemInList(IRedisList<T> toList, int listIndex, T value)
        {
            client.LSet(toList.Id, listIndex, SerializeValue(value));
        }

        public void InsertBeforeItemInList(IRedisList<T> toList, T pivot, T value)
        {
            client.LInsert(toList.Id, insertBefore: true, pivot: SerializeValue(pivot), value: SerializeValue(value));
        }

        public void InsertAfterItemInList(IRedisList<T> toList, T pivot, T value)
        {
            client.LInsert(toList.Id, insertBefore: false, pivot: SerializeValue(pivot), value: SerializeValue(value));
        }

        public void EnqueueItemOnList(IRedisList<T> fromList, T item)
        {
            client.LPush(fromList.Id, SerializeValue(item));
        }

        public T DequeueItemFromList(IRedisList<T> fromList)
        {
            return DeserializeValue(client.RPop(fromList.Id));
        }

        public T BlockingDequeueItemFromList(IRedisList<T> fromList, TimeSpan? timeOut)
        {
            var unblockingKeyAndValue = client.BRPop(fromList.Id, (int)timeOut.GetValueOrDefault().TotalSeconds);
            return unblockingKeyAndValue.Length == 0
                ? default(T)
                : DeserializeValue(unblockingKeyAndValue[1]);
        }

        public void PushItemToList(IRedisList<T> fromList, T item)
        {
            client.RPush(fromList.Id, SerializeValue(item));
        }

        public T PopItemFromList(IRedisList<T> fromList)
        {
            return DeserializeValue(client.RPop(fromList.Id));
        }

        public T BlockingPopItemFromList(IRedisList<T> fromList, TimeSpan? timeOut)
        {
            var unblockingKeyAndValue = client.BRPop(fromList.Id, (int)timeOut.GetValueOrDefault().TotalSeconds);
            return unblockingKeyAndValue.Length == 0
                ? default(T)
                : DeserializeValue(unblockingKeyAndValue[1]);
        }

        public T PopAndPushItemBetweenLists(IRedisList<T> fromList, IRedisList<T> toList)
        {
            return DeserializeValue(client.RPopLPush(fromList.Id, toList.Id));
        }

        public T BlockingPopAndPushItemBetweenLists(IRedisList<T> fromList, IRedisList<T> toList, TimeSpan? timeOut)
        {
            return DeserializeValue(client.BRPopLPush(fromList.Id, toList.Id, (int)timeOut.GetValueOrDefault().TotalSeconds));
        }
    }
}