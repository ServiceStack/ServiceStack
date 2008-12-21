/*
// $Id: IShardedProviderFactory.cs 258 2008-11-28 17:02:44Z DDNGLOBAL\Pete $
//
// Revision      : $Revision: 258 $
// Modified Date : $LastChangedDate: 2008-11-28 17:02:44 +0000 (Fri, 28 Nov 2008) $
// Modified By   : $LastChangedBy: DDNGLOBAL\Pete $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

namespace ServiceStack.DataAccess.Shards
{
	public interface IShardedProviderFactory
	{
		bool ShardExists(int shardId);

		void SetShard(int shardId, string connectionString);

		IPersistenceProvider CreateProvider(int shardId);
	}
}
