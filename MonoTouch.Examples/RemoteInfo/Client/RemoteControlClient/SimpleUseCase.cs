
using System;
using System.Data;
using ServiceStack.DataAccess;
using ServiceStack.OrmLite.Sqlite;

namespace RemoteControlClient
{

	public class SimpleUseCase
	{
		public void TestFixtureSetUp()
		{
			//Inject your database provider here
			OrmLiteExtensions.DialectProvider = new SqliteOrmLiteDialectProvider();
		}

		public class User
		{
			public long Id { get; set; }

			[Index]
			public string Name { get; set; }

			public DateTime CreatedDate { get; set; }
		}

		public void Simple_CRUD_example()
		{
			using (IDbConnection db = ":memory:".OpenDbConnection())
			using (IDbCommand dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<User>(false);

				dbCmd.Insert(new User { Id = 1, Name = "A", CreatedDate = DateTime.Now });
				dbCmd.Insert(new User { Id = 2, Name = "B", CreatedDate = DateTime.Now });
				dbCmd.Insert(new User { Id = 3, Name = "B", CreatedDate = DateTime.Now });

				var rowsB = dbCmd.Select<User>("Name = {0}", "B");

				Console.WriteLine(string.Format("rowsB, Has.Count({0})", rowsB.Count));

				var rowIds = rowsB.ConvertAll(x => x.Id.ToString());
				Console.WriteLine(string.Format("rowIds: {0}", string.Join(", ", rowIds.ToArray())));

				rowsB.ForEach(x => dbCmd.Delete(x));

				rowsB = dbCmd.Select<User>("Name = {0}", "B");
				Console.WriteLine(string.Format("rowsB, Has.Count({0})", rowsB.Count));

				var rowsLeft = dbCmd.Select<User>();
				Console.WriteLine(string.Format("rowsLeft, Has.Count({0})", rowsLeft.Count));

				Console.WriteLine(string.Format("rowsLeft[0].Name = {0}", rowsLeft[0].Name));
			}
		}
	}
	
	
}
