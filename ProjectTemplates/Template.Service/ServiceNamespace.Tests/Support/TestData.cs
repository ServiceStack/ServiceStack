/*
// $Id$
//
// Revision      : $Revision: 671 $
// Modified Date : $LastChangedDate: 2008-12-22 15:52:00 +0000 (Mon, 22 Dec 2008) $
// Modified By   : $LastChangedBy$
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System;
using System.Collections.Generic;
using Ddn.Common.Services.Crypto;
using Ddn.DataAccess;
using @ServiceNamespace@.DataAccess;
using DataModel = @ServiceNamespace@.DataAccess.DataModel;

namespace @ServiceNamespace@.Tests.Support
{
	public static class TestData
	{
		public const string ClientModulusBase64 = "egFNQyG9xGWI9PIGOpVi8cACvsQKDqFY1Gd8UlHNBFkCm6drjfydql49ZV0sG9iTOlH9KVnWeI5uM8AmdL/mtQ==";
		public const string @ModelName@Password = "password";

		public static DataModel.@ModelName@ New@ModelName@
		{
			get
			{
				Guid globalId = Guid.NewGuid();
				string globalIdStr = globalId.ToString("N");
				string userName = string.Format("{0}@{1}.com", globalIdStr.Substring(0, 24), globalIdStr.Substring(24));
				string saltPassword = HashUtils.GenerateSHA1SaltPassword(@ModelName@Password, 0x10);

				return new DataModel.@ModelName@
				{
					Id = 1,
					GlobalId = globalId.ToByteArray(),
					CreatedDate = DateTime.Now,
					CreatedBy = "CreatedBy",
					LastModifiedDate = DateTime.Now,
					LastModifiedBy = "LastModifiedBy",
					@ModelName@Name = userName,
					LastName = "LastName",
					FirstName = "FirstName",
					CanNotifyEmail = 1,
					SingleClickBuyEnabled = 1,
					SaltPassword = saltPassword,
					Email = "user@host.com",
					Country = "Country",
					LanguageCode = "en-GB",
					Balance = 70,
				};
			}
		}

		public static List<DataModel.@ModelName@> Load@ModelName@s(@ServiceName@DataAccessProvider provider, int userCount)
		{
			List<Guid> userGlobalIds = new List<Guid>();

			for (int i = 0; i < userCount; i++)
			{
				var user = TestData.New@ModelName@;
				userGlobalIds.Add(user.GlobalIdGuid);
				provider.Store(user);
			}

			return provider.Get@ModelName@s(null, userGlobalIds, null);
		}
	}
}