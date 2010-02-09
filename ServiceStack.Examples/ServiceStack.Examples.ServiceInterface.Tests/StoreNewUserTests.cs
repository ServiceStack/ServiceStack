using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.DataAccess;
using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;

namespace ServiceStack.Examples.ServiceInterface.Tests
{
	[TestFixture]
	public class StoreNewUserTests : TestHostBase
	{
		readonly StoreNewUser request = new StoreNewUser {
			UserName = "Test",
			Email = "admin@test.com",
			Password = "password"
		};

		[Test]
		public void StoreNewUser_Test()
		{
			var factory = new OrmLiteConnectionFactory(
				InMemoryDb, false, SqliteOrmLiteDialectProvider.Instance);

			using (var dbConn = factory.OpenDbConnection())
			using (var dbCmd = dbConn.CreateCommand())
			{
				dbCmd.CreateTable<User>(true);

				var service = new StoreNewUserService { ConnectionFactory = factory };
				var response = (StoreNewUserResponse)service.Execute(request);

				Assert.That(response.UserId, Is.EqualTo(1));

				var storedUser = dbCmd.First<User>("UserName = {0}", request.UserName);
				Assert.That(storedUser.Email, Is.EqualTo(request.Email));
				Assert.That(storedUser.Password, Is.EqualTo(request.Password));
			}
		}

		[Test]
		public void Existing_user_returns_error_response()
		{
			var factory = new OrmLiteConnectionFactory(
				InMemoryDb, false, SqliteOrmLiteDialectProvider.Instance);

			using (var dbConn = factory.OpenDbConnection())
			using (var dbCmd = dbConn.CreateCommand())
			{
				dbCmd.CreateTable<User>(true);
				dbCmd.Insert(new User { UserName = request.UserName });

				var service = new StoreNewUserService { ConnectionFactory = factory };
				var response = (StoreNewUserResponse)service.Execute(request);

				Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo("UserNameMustBeUnique"));
			}
		}

	}

}
