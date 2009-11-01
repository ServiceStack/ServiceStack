using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.DataAccess;
using ServiceStack.Examples.ServiceInterface.Types;

namespace ServiceStack.Examples.ServiceInterface.Tests
{
	[TestFixture]
	public class GetUsersTests : TestBase
	{
		[Test]
		public void GetUsers_Test()
		{
			var request = new GetUsers {
				UserIds = new ArrayOfLong(1, 2),
				UserNames = new ArrayOfString("User1", "User2")
			};

			var mockPersistence = new Mock<IQueryablePersistenceProvider>();

			mockPersistence.Expect(x => x.GetByIds<User>(It.IsAny<ICollection>())).Returns(
				new List<User> { new User { Id = 1 }, new User { Id = 2 } });

			mockPersistence.Expect(x => x.FindByValues<User>("UserName", It.IsAny<ICollection>())).Returns(
				new List<User> { new User { UserName = "User1" }, new User { UserName = "User2" } });

			var providerManagerObject = GetMockProviderManagerObject(mockPersistence);

			var handler = new GetUsersHandler(providerManagerObject);

			var response = (GetUsersResponse)handler.Execute(request);

			Assert.That(response.Users.Count, Is.EqualTo(4));
		}
	}

}
