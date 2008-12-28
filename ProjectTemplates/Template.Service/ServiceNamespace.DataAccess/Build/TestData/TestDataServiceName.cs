/*
// $Id: TestData@ServiceName@.cs 580 2008-12-17 12:06:53Z DDNGLOBAL\Pete $
//
// Revision      : $Revision: 580 $
// Modified Date : $LastChangedDate: 2008-12-17 12:06:53 +0000 (Wed, 17 Dec 2008) $ 
// Modified By   : $LastChangedBy: DDNGLOBAL\Pete $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System;
using Ddn.Common.Services.Crypto;
using @ServiceNamespace@.DataAccess.DataModel;

namespace @ServiceNamespace@.DataAccess.Build.TestData
{
	public class TestData@ServiceName@
	{
		public @ModelName@ @ModelName@
		{
			get
			{
				string saltPassword = HashUtils.GenerateSHA1SaltPassword("password", 0x10);

				var entity = new @ModelName@ {
					Id = 1,
					GlobalId = Guid.NewGuid().ToByteArray(),
					CreatedDate = DateTime.Now,
					CreatedBy = "CreatedBy",
					LastModifiedDate = DateTime.Now,
					LastModifiedBy = "LastModifiedBy",
					@ModelName@Name = "@ModelName@Name" + new Random().Next(1000),
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
				return entity;
			}
		}

		public CreditCardInfo CreditCardInfo
		{
			get
			{
				var entity = new CreditCardInfo {
					Id = 1,
					CardNumber = "CardNo",
					CardCvv = "Cvv",
					CardExpiryDate = DateTime.Now,
					CardHolderName = "CardName",
					IsActive = 1,
				};
				return entity;
			}
		}

		public @ModelName@Product @ModelName@Product
		{
			get
			{
				var entity = new @ModelName@Product {
					CreatedDate = DateTime.Now,
					CreatedBy = "CreatedBy",
					LastModifiedDate = DateTime.Now,
					LastModifiedBy = "LastModifiedBy",
					ProductId = 1,
					AssetId = 1,
					ParentId = 2,
					PurchaseDate = DateTime.Now,
					DownloadStartDate = DateTime.Now,
					DownloadCompleteDate = DateTime.Now
				};
				return entity;
			}
		}

		public DownloadList DownloadList
		{
			get
			{
				var entity = new DownloadList {
					Id = 1,
					AssetId = 1,
					CreatedDate = DateTime.Now,
					ProductId = 1,
					SortOrder = 1,
				};
				return entity;
			}
		}

		public @ModelName@Set @ModelName@Set
		{
			get
			{
				var entity = new @ModelName@Set {
					Id = 1,
					CreatedBy = "CreatedBy",
					CreatedDate = DateTime.Now,
					LastModifiedBy = "LastModifiedBy",
					LastModifiedDate = DateTime.Now,
					Name = "CardName",
					Type = "Type",
				};
				return entity;
			}
		}

		public @ModelName@SetProduct @ModelName@SetProduct
		{
			get
			{
				var entity = new @ModelName@SetProduct {
					Id = 1,
					ProductId = 1,
					SortOrder = 1,
				};
				return entity;
			}
		}

		public Genre Genre
		{
			get
			{
				var entity = new Genre {
					Id = 1,
					Name = "CardName",
				};
				return entity;
			}
		}

		public Tag Tag
		{
			get
			{
				var entity = new Tag {
					Id = 1,
					Name = "CardName",
				};
				return entity;
			}
		}

		public @ModelName@Order @ModelName@Order
		{
			get
			{
				var entity = new @ModelName@Order {
					Id = 1,
					@ModelName@GlobalId = Guid.NewGuid().ToByteArray(),
					CreatedBy = "CreatedBy",
					CreatedDate = DateTime.Now,
					Total = 1,
				};
				return entity;
			}
		}

		public @ModelName@OrderLineItem @ModelName@OrderLineItem
		{
			get
			{
				var entity = new @ModelName@OrderLineItem {
					Id = 1,
					Name = "CardName",
					Quantity = 1,
					SubTotal = 1,
					Total = 1,
					UnitPrice = 1,
					Vat = 1,
				};
				return entity;
			}
		}
	}
}
