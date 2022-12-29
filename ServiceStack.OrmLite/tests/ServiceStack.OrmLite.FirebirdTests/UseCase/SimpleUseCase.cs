using System;
using System.Collections.Generic;
using System.Data;
using NUnit.Framework;
using ServiceStack.OrmLite.Firebird;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.FirebirdTests
{
	[TestFixture]
	public class SimpleUseCase
	{
		[OneTimeSetUp]
		public void TestFixtureSetUp()
		{
			//Inject your database provider here
			OrmLiteConfig.DialectProvider = new FirebirdOrmLiteDialectProvider();
		}
		
		
		public class User
		{	
			public long Id { get; set; }

			[Index]
			
			public string Name { get; set; }	
			
			public DateTime CreatedDate { get; set; }
		}

		public class GuidId
		{
			public Guid Id { get; set; }
		}

		[Test]
		public void Simple_CRUD_example()
		{
			using (IDbConnection db = "User=SYSDBA;Password=masterkey;Database=ormlite-tests.fdb;DataSource=localhost;Dialect=3;charset=ISO8859_1;".OpenDbConnection())
			{
				db.CreateTable<User>(true);
				
				db.Insert(new User { Id = 1, Name = "A", CreatedDate = DateTime.Now });
				db.Insert(new User { Id = 2, Name = "B", CreatedDate = DateTime.Now });
				db.Insert(new User { Id = 3, Name = "B", CreatedDate = DateTime.Now });
				
				var rowsB = db.Select<User>("Name = @name", new { name = "B" });

				Assert.That(rowsB, Has.Count.EqualTo(2));

				var rowIds = rowsB.ConvertAll(x => x.Id);
				Assert.That(rowIds, Is.EquivalentTo(new List<long> { 2, 3 }));

				rowsB.ForEach(x => db.Delete(x));

				rowsB = db.Select<User>("Name = @name", new { name = "B" });
				Assert.That(rowsB, Has.Count.EqualTo(0));

				var rowsLeft = db.Select<User>();
				Assert.That(rowsLeft, Has.Count.EqualTo(1));

				Assert.That(rowsLeft[0].Name, Is.EqualTo("A"));

				db.CreateTable<GuidId>(true);
				Guid g = Guid.NewGuid();
				db.Insert(new GuidId { Id = g });

				GuidId gid = db.SingleById<GuidId>(g);
				Assert.That(g == gid.Id);
			}
		}

	}
	
}
