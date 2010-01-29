using System.ComponentModel.DataAnnotations;
using System.Data;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.DataAnnotations;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.OrmLite.Sqlite;

namespace ServiceStack.OrmLite.Tests
{
	[TestFixture]
	public class ShippersExample
	{
		static ShippersExample()
		{
			OrmLiteConfig.DialectProvider = SqliteOrmLiteDialectProvider.Instance;
		}


		[Alias("Shippers")]
		public class Shipper
			: IHasId<int>
		{
			[AutoIncrement]
			[Alias("ShipperID")]
			public int Id { get; set; }

			[Required]
			[Index(Unique = true)]
			[StringLength(40)]
			public string CompanyName { get; set; }

			[StringLength(24)]
			public string Phone { get; set; }

			[References(typeof(ShipperType))]
			public int ShipperTypeId { get; set; }
		}

		[Alias("ShipperTypes")]
		public class ShipperType
			: IHasId<int>
		{
			[AutoIncrement]
			[Alias("ShipperTypeID")]
			public int Id { get; set; }

			[Required]
			[Index(Unique = true)]
			[StringLength(40)]
			public string Name { get; set; }
		}

		public class SubsetOfShipper
		{
			public int ShipperId { get; set; }
			public string CompanyName { get; set; }
		}

		public class ShipperTypeCount
		{
			public int ShipperTypeId { get; set; }
			public int Total { get; set; }
		}


		[Test]
		public void Shippers_UseCase()
		{
			using (var dbConn = ":memory:".OpenDbConnection())
			using (var dbCmd = dbConn.CreateCommand())
			{
				const bool overwrite = false;
				dbCmd.CreateTables(overwrite, typeof(Shipper), typeof(ShipperType));

				int trainsTypeId, planesTypeId;

				//Playing with transactions
				using (var dbTrans = dbCmd.BeginTransaction())
				{
					dbCmd.Insert(new ShipperType { Name = "Trains" });
					trainsTypeId = (int)dbCmd.GetLastInsertId();

					dbCmd.Insert(new ShipperType { Name = "Planes" });
					planesTypeId = (int)dbCmd.GetLastInsertId();

					dbTrans.Commit();
				}
				using (var dbTrans = dbCmd.BeginTransaction(IsolationLevel.ReadCommitted))
				{
					dbCmd.Insert(new ShipperType { Name = "Automobiles" });
					Assert.That(dbCmd.Select<ShipperType>(), Has.Count(3));

					dbTrans.Rollback();
				}
				Assert.That(dbCmd.Select<ShipperType>(), Has.Count(2));


				//Performing standard Insert's and Selects
				dbCmd.Insert(new Shipper { CompanyName = "Trains R Us", Phone = "555-TRAINS", ShipperTypeId = trainsTypeId });
				dbCmd.Insert(new Shipper { CompanyName = "Planes R Us", Phone = "555-PLANES", ShipperTypeId = planesTypeId });
				dbCmd.Insert(new Shipper { CompanyName = "We do everything!", Phone = "555-UNICORNS", ShipperTypeId = planesTypeId });

				Assert.That(dbCmd.Select<Shipper>("CompanyName = {0} OR Phone = {1}", "Trains R Us", "555-UNICORNS"), Has.Count(2));
				Assert.That(dbCmd.Select<Shipper>("ShipperTypeId = {0}", trainsTypeId), Has.Count(1));
				Assert.That(dbCmd.Select<Shipper>("ShipperTypeId = {0}", planesTypeId), Has.Count(2));


				//Performing custom queries
				//Select only a subset from the table
				var partialRows = dbCmd.Select<SubsetOfShipper>(typeof (Shipper), "ShipperTypeId = {0}", planesTypeId);
				Assert.That(partialRows, Has.Count(2));

				//Select into another POCO class that matches sql
				var rows = dbCmd.Select<ShipperTypeCount>(
					"SELECT ShipperTypeId, COUNT(*) AS Total FROM Shippers GROUP BY ShipperTypeId ORDER BY COUNT(*)");

				Assert.That(rows, Has.Count(2));
				Assert.That(rows[0].ShipperTypeId, Is.EqualTo(trainsTypeId));
				Assert.That(rows[0].Total, Is.EqualTo(1));
				Assert.That(rows[1].ShipperTypeId, Is.EqualTo(planesTypeId));
				Assert.That(rows[1].Total, Is.EqualTo(2));
			}
		}


	}


}