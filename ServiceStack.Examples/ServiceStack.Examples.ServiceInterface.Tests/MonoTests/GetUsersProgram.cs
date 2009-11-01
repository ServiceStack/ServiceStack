using System;
using ServiceStack.DataAccess.Db4oProvider;
using ServiceStack.Examples.ServiceInterface.Types;

namespace ServiceStack.Examples.ServiceInterface.Tests.MonoTests
{
	public class GetUsersProgram : TestProgramBase
	{
		public static void Main()
		{
			using (var db4OManager = new Db4OFileProviderManager("test.db4o"))
			{
				var handler = new GetUsersHandler(db4OManager);

				var response = (GetUsersResponse)handler.Execute(
					new GetUsers {
						UserNames = new ArrayOfString("userName")
					});

				if (response.Users.Count > 0)
				{
					var user = response.Users[0];
					Console.WriteLine("User: {0}, {1}, {2}", user.UserName, user.Password, user.Email);
				}
				else
				{
					Console.WriteLine("User does not exist");
				}
			}

		}
	}
}