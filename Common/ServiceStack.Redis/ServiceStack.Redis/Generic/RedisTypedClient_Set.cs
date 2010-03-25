using System.Collections.Generic;
using System.Linq;
using ServiceStack.Common.Extensions;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.Redis.Generic
{
	internal partial class RedisTypedClient<T>
	{
		public IHasNamed<IRedisSet<T>> Sets { get; set; }

		public int Db
		{
			get { return client.Db; }
			set { client.Db = value; }
		}

		internal class RedisClientSets
			: IHasNamed<IRedisSet<T>>
		{
			private readonly RedisTypedClient<T> client;

			public RedisClientSets(RedisTypedClient<T> client)
			{
				this.client = client;
			}

			public IRedisSet<T> this[string setId]
			{
				get
				{
					return new RedisClientSet<T>(client, setId);
				}
				set
				{
					var col = this[setId];
					col.Clear();
					col.CopyTo(value.ToArray(), 0);
				}
			}
		}

		private HashSet<T> CreateHashSet(byte[][] multiDataList)
		{
			var results = new HashSet<T>();
			foreach (var multiData in multiDataList)
			{
				results.Add(FromBytes(multiData));
			}
			return results;
		}

		public List<T> GetRangeFromSortedSet(IRedisSet<T> fromSet, int startingFrom, int endingAt)
		{
			var multiDataList = client.Sort(fromSet.Id, startingFrom, endingAt, true, false);
			return CreateList(multiDataList);
		}

		public HashSet<T> GetAllFromSet(IRedisSet<T> fromSet)
		{
			var multiDataList = client.SMembers(fromSet.Id);
			return CreateHashSet(multiDataList);
		}

		public void AddToSet(IRedisSet<T> toSet, T value)
		{
			client.SAdd(toSet.Id, ToBytes(value));
		}

		public void RemoveFromSet(IRedisSet<T> fromSet, T value)
		{
			client.SRem(fromSet.Id, ToBytes(value));
		}

		public T PopFromSet(IRedisSet<T> fromSet)
		{
			return FromBytes(client.SPop(fromSet.Id));
		}

		public void MoveBetweenSets(IRedisSet<T> fromSet, IRedisSet<T> toSet, T value)
		{
			client.SMove(fromSet.Id, toSet.Id, ToBytes(value));
		}

		public int GetSetCount(IRedisSet<T> set)
		{
			return client.SCard(set.Id);
		}

		public bool SetContainsValue(IRedisSet<T> set, T value)
		{
			return client.SIsMember(set.Id, ToBytes(value)) == 1;
		}

		public HashSet<T> GetIntersectFromSets(params IRedisSet<T>[] sets)
		{
			var multiDataList = client.SInter(sets.ConvertAll(x => x.Id).ToArray());
			return CreateHashSet(multiDataList);
		}

		public void StoreIntersectFromSets(IRedisSet<T> intoSet, params IRedisSet<T>[] sets)
		{
			client.SInterStore(intoSet.Id, sets.ConvertAll(x => x.Id).ToArray());
		}

		public HashSet<T> GetUnionFromSets(params IRedisSet<T>[] sets)
		{
			var multiDataList = client.SUnion(sets.ConvertAll(x => x.Id).ToArray());
			return CreateHashSet(multiDataList);
		}

		public void StoreUnionFromSets(IRedisSet<T> intoSet, params IRedisSet<T>[] sets)
		{
			client.SUnionStore(intoSet.Id, sets.ConvertAll(x => x.Id).ToArray());
		}

		public HashSet<T> GetDifferencesFromSet(IRedisSet<T> fromSet, params IRedisSet<T>[] withSets)
		{
			var multiDataList = client.SDiff(fromSet.Id, withSets.ConvertAll(x => x.Id).ToArray());
			return CreateHashSet(multiDataList);
		}

		public void StoreDifferencesFromSet(IRedisSet<T> intoSet, IRedisSet<T> fromSet, params IRedisSet<T>[] withSets)
		{
			client.SDiffStore(intoSet.Id, fromSet.Id, withSets.ConvertAll(x => x.Id).ToArray());
		}

		public T GetRandomEntryFromSet(IRedisSet<T> fromSet)
		{
			return FromBytes(client.SRandMember(fromSet.Id));
		}
		
	}
}