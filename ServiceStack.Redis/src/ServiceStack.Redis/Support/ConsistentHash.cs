using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ServiceStack.Redis.Support
{
	public class ConsistentHash<T>
	{
		// mutiple replicas of each node improves distribution
		private const int Replicas = 200;

		// default hash function
		private readonly Func<string, ulong> _hashFunction = Md5Hash;
		private readonly SortedDictionary<ulong, T> _circle = new SortedDictionary<ulong, T>();

		public ConsistentHash()
		{
		}

		public ConsistentHash(IEnumerable<KeyValuePair<T, int>> nodes)
			: this(nodes, null)
		{
		}

		public ConsistentHash(IEnumerable<KeyValuePair<T, int>> nodes, Func<string, ulong> hashFunction)
		{
			if (hashFunction != null)
				_hashFunction = hashFunction;

			foreach (var node in nodes)
				AddTarget(node.Key, node.Value);
		}

		public T GetTarget(string key)
		{
			ulong hash = _hashFunction(key);
			ulong firstNode = ModifiedBinarySearch(_circle.Keys.ToArray(), hash);
			return _circle[firstNode];
		}

		/// <summary>
		///  Adds a node and maps points across the circle
		/// </summary>
		/// <param name="node"> node to add </param>
		/// <param name="weight"> An arbitrary number, specifies how often it occurs relative to other targets. </param>
		public void AddTarget(T node, int weight)
		{
			// increase the replicas of the node by weight
			int repeat = weight > 0 ? weight * Replicas : Replicas;

			for (int i = 0; i < repeat; i++)
			{
				string identifier = node.GetHashCode().ToString() + "-" + i;
				ulong hashCode = _hashFunction(identifier);
				_circle.Add(hashCode, node);
			}
		}

		/// <summary>
		///   A variation of Binary Search algorithm. Given a number, matches the next highest number from the sorted array. 
		///   If a higher number does not exist, then the first number in the array is returned.
		/// </summary>
		/// <param name="sortedArray"> a sorted array to perform the search </param>
		/// <param name="val"> number to find the next highest number against </param>
		/// <returns> next highest number </returns>
		public static ulong ModifiedBinarySearch(ulong[] sortedArray, ulong val)
		{
			int min = 0;
			int max = sortedArray.Length - 1;

			if (val < sortedArray[min] || val > sortedArray[max])
				return sortedArray[0];

			while (max - min > 1)
			{
				int mid = (max + min) / 2;
				if (sortedArray[mid] >= val)
				{
					max = mid;
				}
				else
				{
					min = mid;
				}
			}

			return sortedArray[max];
		}

		/// <summary>
		///   Given a key, generates an unsigned 64 bit hash code using MD5
		/// </summary>
		/// <param name="key"> </param>
		/// <returns> </returns>
		public static ulong Md5Hash(string key)
		{
			using (var hash = MD5.Create())
			{
				byte[] data = hash.ComputeHash(Encoding.UTF8.GetBytes(key));
				var a = BitConverter.ToUInt64(data, 0);
				var b = BitConverter.ToUInt64(data, 8);
				ulong hashCode = a ^ b;
				return hashCode;
			}
		}
	}
}
