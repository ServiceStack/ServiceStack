using System;
using System.Collections.Generic;
using System.Data;

using Ddn.DataAccess;
using @ServiceNamespace@.DataAccess.DataModel;

namespace @ServiceNamespace@.DataAccess
{
	public class @ServiceName@DataAccessProvider
	{
		public @ServiceName@DataAccessProvider(IPersistenceProvider provider)
		{
			Provider = provider;
		}

		private IPersistenceProvider Provider { get; set; }

		/// <summary>
		/// Creates the user.
		/// 
		/// Also sets the audit properties here, which can be overriden later.
		/// </summary>
		/// <param name="userName">Name of the user.</param>
		/// <returns></returns>
		public @ModelName@ CreateNew@ModelName@(string userName)
		{
			var now = DateTime.Now;
			var user = new @ModelName@ {
				@ModelName@Name = userName,
                CreatedBy = userName,
				CreatedDate = now,
				LastModifiedBy = userName,
				LastModifiedDate = now,
			};
			return user;
		}

		public @ModelName@ Get@ModelName@(int userId)
		{
			return Provider.GetById<@ModelName@>((uint)userId);
		}

		public @ModelName@ Get@ModelName@(Guid globalId)
		{
			return Provider.FindByValue<@ModelName@>("GlobalId", globalId.ToByteArray());
		}

		/// <summary>
		/// Get a union of users identified by the ids supplied
		/// </summary>
		/// <param name="userIds"></param>
		/// <param name="globalIds"></param>
		/// <param name="userNames"></param>
		/// <returns></returns>
		public List<@ModelName@> Get@ModelName@s(List<int> userIds, List<Guid> globalIds, List<string> userNames)
		{
			//This can be optimized to use 1 query
			var results = new List<@ModelName@>();
			if (userIds != null && userIds.Count > 0)
			{
				results.AddRange(Provider.GetByIds<@ModelName@>(userIds));
			}
			if (globalIds != null && globalIds.Count > 0)
			{
				results.AddRange(Provider.FindByValues<@ModelName@>("GlobalId", globalIds.ConvertAll(x => x.ToByteArray())));
			}
			if (userNames != null && userNames.Count > 0)
			{
				results.AddRange(Provider.FindByValues<@ModelName@>("@ModelName@Name", userNames));
			}
			return results;
		}

		public @ModelName@ Get@ModelName@By@ModelName@Name(string userName)
		{
			return Provider.FindByValue<@ModelName@>("@ModelName@Name", userName);
		}

		public void Store(object entity)
		{
			Provider.Save(entity);
		}

		public ITransactionContext BeginTransaction()
		{
			return Provider.BeginTransaction();
		}
	}
}
