using System.Data;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;

namespace ServiceStack.OrmLite.Tests
{
	[TestFixtureOrmLite]
	public class ShippersExample : OrmLiteProvidersTestBase
	{
		public ShippersExample(DialectContext context) : base(context) {}

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
			public int ShipperID { get; set; }
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
            using (IDbConnection db = OpenDbConnection())
			{
                db.DropTable<Shipper>();
                db.DropTable<ShipperType>();

                db.CreateTable<ShipperType>();
                db.CreateTable<Shipper>();

                var trainsType = new ShipperType { Name = "Trains" };
                var planesType = new ShipperType { Name = "Planes" };

				//Playing with transactions
				using (IDbTransaction dbTrans = db.OpenTransaction())
				{
                    db.Save(trainsType);
                    db.Save(planesType);

					dbTrans.Commit();
				}

				using (IDbTransaction dbTrans = db.OpenTransaction(IsolationLevel.ReadCommitted))
				{
					db.Insert(new ShipperType { Name = "Automobiles" });
					Assert.That(db.Select<ShipperType>(), Has.Count.EqualTo(3));
				}
				Assert.That(db.Select<ShipperType>(), Has.Count.EqualTo(2));


				// Performing standard Insert's and Selects
				db.Insert(new Shipper { CompanyName = "Trains R Us", Phone = "555-TRAINS", ShipperTypeId = trainsType.Id });
				db.Insert(new Shipper { CompanyName = "Planes R Us", Phone = "555-PLANES", ShipperTypeId = planesType.Id });
				db.Insert(new Shipper { CompanyName = "We do everything!", Phone = "555-UNICORNS", ShipperTypeId = planesType.Id });

                var trainsAreUs = db.Single<Shipper>(q => q.ShipperTypeId == trainsType.Id);
                Assert.That(trainsAreUs.CompanyName, Is.EqualTo("Trains R Us"));

                trainsAreUs = db.Single<Shipper>("ShipperTypeId".SqlColumn(DialectProvider) + " = @Id".PreNormalizeSql(db), new { trainsType.Id });
				Assert.That(trainsAreUs.CompanyName, Is.EqualTo("Trains R Us"));


                Assert.That(db.Select<Shipper>(q => q.CompanyName == "Trains R Us" || q.Phone == "555-UNICORNS"), Has.Count.EqualTo(2));
                Assert.That(db.Select<Shipper>("CompanyName".SqlColumn(DialectProvider) + " = @company OR Phone = @phone".PreNormalizeSql(db), 
                    new { company = "Trains R Us", phone = "555-UNICORNS" }), Has.Count.EqualTo(2));

                Assert.That(db.Select<Shipper>(q => q.ShipperTypeId == planesType.Id), Has.Count.EqualTo(2));
                Assert.That(db.Select<Shipper>("ShipperTypeId".SqlColumn(DialectProvider) + " = @Id".PreNormalizeSql(db), new { planesType.Id }), Has.Count.EqualTo(2));

				//Lets update a record
				trainsAreUs.Phone = "666-TRAINS";
				db.Update(trainsAreUs);
                Assert.That(db.SingleById<Shipper>(trainsAreUs.Id).Phone, Is.EqualTo("666-TRAINS"));
				
				//Then make it disappear
				db.Delete(trainsAreUs);
                Assert.That(db.SingleById<Shipper>(trainsAreUs.Id), Is.Null);

				//And bring it back again
				db.Insert(trainsAreUs);


				//Performing custom queries
				//Select only a subset from the table
                var partialColumns = db.Select<SubsetOfShipper>(db.From<Shipper>().Where(q => q.ShipperTypeId == planesType.Id));
                Assert.That(partialColumns, Has.Count.EqualTo(2));

                partialColumns = db.Select<SubsetOfShipper>(typeof(Shipper), "ShipperTypeId".SqlColumn(DialectProvider) + " = @Id".PreNormalizeSql(db), new { planesType.Id });
				Assert.That(partialColumns, Has.Count.EqualTo(2));

				//Select into another POCO class that matches sql
                var rows = db.SqlList<ShipperTypeCount>(
                    "SELECT {0}, COUNT(*) AS Total FROM Shippers GROUP BY {0} ORDER BY Total".Fmt("ShipperTypeId".SqlColumn(DialectProvider)));

                Assert.That(rows, Has.Count.EqualTo(2));
                Assert.That(rows[0].ShipperTypeId, Is.EqualTo(trainsType.Id));
                Assert.That(rows[0].Total, Is.EqualTo(1));
                Assert.That(rows[1].ShipperTypeId, Is.EqualTo(planesType.Id));
                Assert.That(rows[1].Total, Is.EqualTo(2));

                TestCustomGroupBy(db, trainsType, planesType);


			    //And finally lets quickly clean up the mess we've made:
				db.DeleteAll<Shipper>();
				db.DeleteAll<ShipperType>();

				Assert.That(db.Select<Shipper>(), Has.Count.EqualTo(0));
				Assert.That(db.Select<ShipperType>(), Has.Count.EqualTo(0));
			}
		}

	    private static void TestCustomGroupBy(IDbConnection db, ShipperType trainsType, ShipperType planesType)
	    {
	        var rows = db.SqlList<ShipperTypeCount>(
	            db.From<Shipper>()
	              .GroupBy(x => x.ShipperTypeId)
	              .OrderBy("Total")
	              .Select(x => new {x.ShipperTypeId, Total = Sql.As(Sql.Count("*"), "Total")}));

	        Assert.That(rows, Has.Count.EqualTo(2));
	        Assert.That(rows[0].ShipperTypeId, Is.EqualTo(trainsType.Id));
	        Assert.That(rows[0].Total, Is.EqualTo(1));
	        Assert.That(rows[1].ShipperTypeId, Is.EqualTo(planesType.Id));
	        Assert.That(rows[1].Total, Is.EqualTo(2));

	        var qGroup = db.From<Shipper>()
	            .GroupBy(x => x.ShipperTypeId)
	            .OrderBy("2")
	            .Select(x => new {x.ShipperTypeId, Total = Sql.Count("*")});

	        var rowsGroup = db.Dictionary<int, int>(qGroup);

	        Assert.That(rowsGroup, Has.Count.EqualTo(2));
	        Assert.That(rowsGroup[trainsType.Id], Is.EqualTo(1));
	        Assert.That(rowsGroup[planesType.Id], Is.EqualTo(2));
	    }
	}


}