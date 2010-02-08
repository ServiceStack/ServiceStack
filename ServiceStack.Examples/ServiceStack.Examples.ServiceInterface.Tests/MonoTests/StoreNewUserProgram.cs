using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.DataAccess.Db4oProvider;
using ServiceStack.Examples.ServiceInterface.Types;

namespace ServiceStack.Examples.ServiceInterface.Tests.MonoTests
{
	public class StoreNewUserProgram : TestProgramBase
	{
		public static void Main()
		{
			var testConfig = new ExampleConfig { ConnectionString = ":memory:" };
			var handler = new StoreNewUserService(testConfig);

			handler.Execute(new StoreNewUser {
				Email = "email",
				UserName = "userName",
				Password = "password"
			});

		}
	}
}
