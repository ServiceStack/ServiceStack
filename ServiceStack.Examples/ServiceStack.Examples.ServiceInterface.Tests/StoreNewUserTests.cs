using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.DataAccess;
using ServiceStack.Examples.ServiceInterface.Types;

namespace ServiceStack.Examples.ServiceInterface.Tests
{
	[TestFixture]
	public class StoreNewUserTests : TestBase
	{
		readonly StoreNewUser request = new StoreNewUser {
			UserName = "Test",
			Email = "admin@test.com",
			Password = "password"
		};

		[Test]
		public void StoreNewUser_Test()
		{
			var mockPersistence = new Mock<IQueryablePersistenceProvider>();

			mockPersistence.Expect(x => x.Query(It.IsAny<Predicate<User>>())).Returns(
				new List<User>());

			mockPersistence.Expect(x => x.Store(It.IsAny<User>())).Callback(
				delegate(User user) {
					user.Id = 10;
				});

			TestAppHost.Instance.RegisterMockPersistenceProvider(mockPersistence.Object);

			var handler = new StoreNewUserHandler();
			var response = (StoreNewUserResponse)handler.Execute(CreateOperationContext(request));

			mockPersistence.VerifyAll();

			Assert.That(response.UserId, Is.EqualTo(10));
		}

		[Test]
		public void Existing_user_returns_error_response()
		{
			var mockPersistence = new Mock<IQueryablePersistenceProvider>();

			mockPersistence.Expect(x => x.Query(It.IsAny<Predicate<User>>())).Returns(
				new[] { new User { UserName = request.UserName }, }.ToList());

			TestAppHost.Instance.RegisterMockPersistenceProvider(mockPersistence.Object);

			var handler = new StoreNewUserHandler();
			var response = (StoreNewUserResponse)handler.Execute(CreateOperationContext(request));

			mockPersistence.VerifyAll();

			Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo("UserAlreadyExists"));
		}

	}

}
