using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace ServiceStack.DataAccess.Db4oProvider.Tests
{
	[TestFixture]
	public class Db4OBasicWriteTests : Db4OTestBase
	{

		[Test]
		public void Add_Update_and_Store()
		{
			var newCustomer = new Customer(100, "New", "Customer");
			provider.Store(newCustomer);

			var storedCustomer = provider.GetById<Customer>(100);
			Assert.That(storedCustomer.Name, Is.EqualTo(newCustomer.Name));

			storedCustomer.FirstName = "Modified";
			provider.Store(storedCustomer);

			var modifiedCustomer = provider.GetById<Customer>(100);
			Assert.That(modifiedCustomer.FirstName, Is.EqualTo("Modified"));

			provider.Delete(modifiedCustomer);
			var deletedCustomer = provider.GetById<Customer>(100);
			Assert.That(deletedCustomer, Is.Null);
		}

		public class User
		{
			public long Id { get; set; }
			public string Name { get; set; }
		}

		[Test]
		public void Store_sets_id_on_unassigned_long_id()
		{
			var newUser = new User { Name = "UserWithoutId" };
			provider.Store(newUser);

			var user = provider.FindByValue<User>("Name", newUser.Name);

			Assert.That(user, Is.Not.Null);
			Assert.That(user.Id, Is.Not.EqualTo(default(long)));
		}

		[Test]
		public void Store_does_not_set_id_on_assigned_long_id()
		{
			var newUser = new User { Id = 1, Name = "UserWithIdSet" };
			provider.Store(newUser);

			var user = provider.FindByValue<User>("Name", newUser.Name);

			Assert.That(user, Is.Not.Null);
			Assert.That(user.Id, Is.EqualTo(newUser.Id));
		}

		[Test]
		public void Can_update_user_retrieved_with_internalId()
		{
			var newUser = new User { Name = "UserWithoutId" };
			provider.Store(newUser);

			var user = provider.FindByValue<User>("Name", newUser.Name);

			Assert.That(user, Is.Not.Null);
			Assert.That(user.Id, Is.Not.EqualTo(default(long)));

			user = provider.GetById<User>(user.Id);
			Assert.That(user, Is.Not.Null);
			var updatedUserName = "UpdatedUser";
			user.Name = updatedUserName;

			provider.Store(user);
			var users = provider.GetByIds<User>(new[] { user.Id });

			Assert.That(users.Count, Is.EqualTo(1));
			Assert.That(users[0].Name, Is.EqualTo(updatedUserName));
		}
	}
}