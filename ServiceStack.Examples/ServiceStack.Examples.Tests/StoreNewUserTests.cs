using NUnit.Framework;
using ServiceStack.Examples.ServiceInterface;
using ServiceStack.Examples.ServiceModel.Operations;
using ServiceStack.Examples.ServiceModel.Types;
using ServiceStack.OrmLite;

namespace ServiceStack.Examples.Tests
{
	[TestFixture]
	public class StoreNewUserTests 
		: TestHostBase
	{
		readonly StoreNewUser request = new StoreNewUser
		{
			UserName = "Test",
			Email = "admin@test.com",
			Password = "password"
		};

		[Test]
		public void StoreNewUser_Test()
		{
			using (var dbConn = ConnectionFactory.OpenDbConnection())
			using (var dbCmd = dbConn.CreateCommand())
			{
				var service = new StoreNewUserService { ConnectionFactory = ConnectionFactory };
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
			using (var dbConn = ConnectionFactory.OpenDbConnection())
			using (var dbCmd = dbConn.CreateCommand())
			{
				dbCmd.Insert(new User { UserName = request.UserName });

				var service = new StoreNewUserService { ConnectionFactory = ConnectionFactory };
				var response = (StoreNewUserResponse)service.Execute(request);

				Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo("UserNameMustBeUnique"));
			}
		}

	}
}