using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.Redis
{
	/// <summary>
	/// Provides a redis connection pool that can be sharded
	/// </summary>
	public class ShardedConnectionPool : PooledRedisClientManager
	{
		/// <summary>
		/// logical name
		/// </summary>
		public readonly string name;

		/// <summary>
		/// An arbitrary weight relative to other nodes
		/// </summary>
		public readonly int weight;

		/// <param name="name">logical name</param>
		/// <param name="weight">An arbitrary weight relative to other nodes</param>
		/// <param name="readWriteHosts">redis nodes</param>
		public ShardedConnectionPool(string name, int weight, params string[] readWriteHosts)
			: base(readWriteHosts)
		{
			this.PoolTimeout = 1000;
			this.name = name;
			this.weight = weight;
		}

		public override int GetHashCode()
		{
			// generate hashcode based on logial name
			// server alias/ip can change without 
			// affecting the consistent hash
			return name.GetHashCode();
		}
	}
}
