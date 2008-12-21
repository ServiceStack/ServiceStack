/*
// $Id: ShardedProviderFactory.cs 258 2008-11-28 17:02:44Z DDNGLOBAL\Pete $
//
// Revision      : $Revision: 258 $
// Modified Date : $LastChangedDate: 2008-11-28 17:02:44 +0000 (Fri, 28 Nov 2008) $
// Modified By   : $LastChangedBy: DDNGLOBAL\Pete $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System.Collections.Generic;
using ServiceStack.Logging;

namespace ServiceStack.DataAccess.Shards
{
	public class ShardedProviderFactory : IShardedProviderFactory
	{
		public IPersistenceProviderManagerFactory ProviderManagerFactory { get; set; }

		private IDictionary<int, IPersistenceProviderManager> ProviderManagersByShardId { get; set; }
		private IDictionary<string, IPersistenceProviderManager> ProviderManagersByConnectionString { get; set; }
		
		private readonly object newConnectionLock = new object();
		private readonly ILog log;

		public ShardedProviderFactory(ILogFactory logFactory)
		{
			this.ProviderManagersByShardId = new Dictionary<int, IPersistenceProviderManager>();
			this.ProviderManagersByConnectionString = new Dictionary<string, IPersistenceProviderManager>();
			this.log = logFactory.GetLogger(GetType());
		}

		public bool ShardExists(int shardId)
		{
			return this.ProviderManagersByShardId.ContainsKey(shardId);
		}

		public void SetShard(int shardId, string connectionString)
		{
			string connectionStringKey = connectionString.ToUpperInvariant();

			// Determine if the shard location has a current connection manager
			var providerManager = this.ProviderManagersByConnectionString.SafeGetValue(connectionStringKey);

			// If the connection manager is not already available create one for the shard location
			if (providerManager == null)
			{
				providerManager = this.ProviderManagerFactory.CreateProviderManager(connectionString);
				this.ProviderManagersByConnectionString[connectionStringKey] = providerManager;
			}

			// Get the previous provider manager if one exists
			var previousProviderManager = this.ProviderManagersByShardId.SafeGetValue(shardId);

			if (previousProviderManager == null || ReferenceEquals(previousProviderManager, providerManager) == false)
			{
				lock (newConnectionLock)
				{
					// Replace the previous provider connection manager for the given shardId with the new one.
					this.ProviderManagersByShardId[shardId] = providerManager;

					// If the previous provider manager is not used then remove it
					this.CleanConnectionManager(previousProviderManager);
				}
			}
		}

		public IPersistenceProvider CreateProvider(int shardId)
		{
			IPersistenceProviderManager providerManager = this.ProviderManagersByShardId.SafeGetValue(shardId);
			return providerManager != null ? providerManager.CreateProvider() : null;
		}

		private void CleanConnectionManager(IPersistenceProviderManager providerManager)
		{
			if (providerManager != null)
			{
				foreach (var currentProviderManager in this.ProviderManagersByShardId.Values)
				{
					if (ReferenceEquals(providerManager, currentProviderManager))
					{
						return;
					}
				}

				this.ProviderManagersByConnectionString.Remove(providerManager.ConnectionString.ToUpperInvariant());
			}
		}
	}
}