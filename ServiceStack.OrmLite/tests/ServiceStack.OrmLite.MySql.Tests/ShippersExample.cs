using System.Data;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.MySql.Tests
{
	[TestFixture]
	public class ShippersExample
	{
		static ShippersExample()
		{
			OrmLiteConfig.DialectProvider = MySqlConfig.DialectProvider;
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
			using (IDbConnection db = MySqlConfig.ConnectionString.OpenDbConnection())
			{
                db.DropTable<Shipper>();
                db.DropTable<ShipperType>();

                db.CreateTable<ShipperType>();
                db.CreateTable<Shipper>();

				int trainsTypeId, planesTypeId;

				//Playing with transactions
				using (IDbTransaction dbTrans = db.OpenTransaction())
				{
					db.Insert(new ShipperType { Name = "Trains" });
					trainsTypeId = (int)db.LastInsertId();

					db.Insert(new ShipperType { Name = "Planes" });
					planesTypeId = (int)db.LastInsertId();

					dbTrans.Commit();
				}
				using (IDbTransaction dbTrans = db.OpenTransaction(IsolationLevel.ReadCommitted))
				{
					db.Insert(new ShipperType { Name = "Automobiles" });
					Assert.That(db.Select<ShipperType>(), Has.Count.EqualTo(3));

					dbTrans.Rollback();
				}
				Assert.That(db.Select<ShipperType>(), Has.Count.EqualTo(2));


				//Performing standard Insert's and Selects
				db.Insert(new Shipper { CompanyName = "Trains R Us", Phone = "555-TRAINS", ShipperTypeId = trainsTypeId });
				db.Insert(new Shipper { CompanyName = "Planes R Us", Phone = "555-PLANES", ShipperTypeId = planesTypeId });
				db.Insert(new Shipper { CompanyName = "We do everything!", Phone = "555-UNICORNS", ShipperTypeId = planesTypeId });

				var trainsAreUs = db.Single<Shipper>("ShipperTypeId = @id", new { id = trainsTypeId });
				Assert.That(trainsAreUs.CompanyName, Is.EqualTo("Trains R Us"));
				Assert.That(db.Select<Shipper>("CompanyName = @company OR Phone = @phone", 
                    new { company = "Trains R Us", phone = "555-UNICORNS" }), Has.Count.EqualTo(2));
				Assert.That(db.Select<Shipper>("ShipperTypeId = @id", new { id = planesTypeId }), Has.Count.EqualTo(2));

				//Lets update a record
				trainsAreUs.Phone = "666-TRAINS";
				db.Update(trainsAreUs);
                Assert.That(db.SingleById<Shipper>(trainsAreUs.Id).Phone, Is.EqualTo("666-TRAINS"));
				
				//Then make it dissappear
				db.Delete(trainsAreUs);
                Assert.That(db.SingleById<Shipper>(trainsAreUs.Id), Is.Null);

				//And bring it back again
				db.Insert(trainsAreUs);


				//Performing custom queries
				//Select only a subset from the table
				var partialColumns = db.Select<SubsetOfShipper>(typeof(Shipper), "ShipperTypeId = @id", new { id = planesTypeId });
				Assert.That(partialColumns, Has.Count.EqualTo(2));

				//Select into another POCO class that matches sql
				var rows = db.Select<ShipperTypeCount>(
					"SELECT ShipperTypeId, COUNT(*) AS Total FROM Shippers GROUP BY ShipperTypeId ORDER BY COUNT(*)");

				Assert.That(rows, Has.Count.EqualTo(2));
				Assert.That(rows[0].ShipperTypeId, Is.EqualTo(trainsTypeId));
				Assert.That(rows[0].Total, Is.EqualTo(1));
				Assert.That(rows[1].ShipperTypeId, Is.EqualTo(planesTypeId));
				Assert.That(rows[1].Total, Is.EqualTo(2));


				//And finally lets quickly clean up the mess we've made:
				db.DeleteAll<Shipper>();
				db.DeleteAll<ShipperType>();

				Assert.That(db.Select<Shipper>(), Has.Count.EqualTo(0));
				Assert.That(db.Select<ShipperType>(), Has.Count.EqualTo(0));
			}
		}
	}
}