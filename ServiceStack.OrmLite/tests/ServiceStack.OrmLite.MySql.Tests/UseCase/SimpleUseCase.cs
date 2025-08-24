using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.MySql.Tests.UseCase;

[TestFixture]
public class SimpleUseCase
{
	public SimpleUseCase()
	{
		//Inject your database provider here
		OrmLiteConfig.DialectProvider = MySqlConfig.DialectProvider;
	}

	public class UserWithIndex
	{
		public long Id { get; set; }

		[ServiceStack.DataAnnotations.Index]
		public string Name { get; set; }

		public DateTime CreatedDate { get; set; }
	}

	[Test]
	public void Simple_CRUD_example()
	{
		using (var db = MySqlConfig.ConnectionString.OpenDbConnection())
		{
			db.CreateTable<UserWithIndex>(true);

			db.Insert(new UserWithIndex { Id = 1, Name = "A", CreatedDate = DateTime.Now });
			db.Insert(new UserWithIndex { Id = 2, Name = "B", CreatedDate = DateTime.Now });
			db.Insert(new UserWithIndex { Id = 3, Name = "B", CreatedDate = DateTime.Now });

			var rowsB = db.Select<UserWithIndex>("Name = @name", new { name = "B" });

			Assert.That(rowsB, Has.Count.EqualTo(2));

			var rowIds = rowsB.ConvertAll(x => x.Id);
			Assert.That(rowIds, Is.EquivalentTo(new List<long> { 2, 3 }));

			rowsB.ForEach(x => db.Delete(x));

			rowsB = db.Select<UserWithIndex>("Name = @name", new { name = "B" });
			Assert.That(rowsB, Has.Count.EqualTo(0));

			var rowsLeft = db.Select<UserWithIndex>();
			Assert.That(rowsLeft, Has.Count.EqualTo(1));

			Assert.That(rowsLeft[0].Name, Is.EqualTo("A"));
		}
	}

}