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
			using (var db4OManager = new Db4OFileProviderManager("test.db4o"))
			{
				var handler = new StoreNewUserService(db4OManager);

				handler.Execute(new StoreNewUser {
					Email = "email",
					UserName = "userName",
					Password = "password"
				});
			}

		}
	}
}
