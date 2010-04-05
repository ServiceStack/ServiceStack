using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.Examples.ServiceInterface.Tests
{
	[TestFixture]
	public class GetUsersTests
		: TestHostBase
	{
		[Test]
		public void GetUsers_Test()
		{
			var request = new GetUsers {
				UserIds = new ArrayOfLong(1, 2),
				UserNames = new ArrayOfString("User3", "User4")
			};

			var factory = new OrmLiteConnectionFactory(
				InMemoryDb, false, SqliteOrmLiteDialectProvider.Instance);

			using (var dbConn = factory.OpenDbConnection())
			using (var dbCmd = dbConn.CreateCommand())
			{
				dbCmd.CreateTable<User>(true);
				dbCmd.Insert(new User { Id = 1, UserName = "User1" });
				dbCmd.Insert(new User { Id = 2, UserName = "User2" });
				dbCmd.Insert(new User { Id = 3, UserName = "User3" });
				dbCmd.Insert(new User { Id = 4, UserName = "User4" });

				var handler = new GetUsersService { ConnectionFactory = factory };

				var response = (GetUsersResponse)handler.Execute(request);

				Assert.That(response.Users.Count, Is.EqualTo(4));
			}
		}
	}

}
