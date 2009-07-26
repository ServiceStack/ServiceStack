using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Examples.ServiceInterface.Types;

namespace ServiceStack.Examples.ServiceInterface.Tests.MonoTests
{
	public class StoreNewUserProgram : TestProgramBase
	{
		public static void Main()
		{
			InitApplicationContext();

			var handler = new StoreNewUserHandler();
			
			handler.Execute(CreateOperationContext(
				new StoreNewUser {
					Email = "email",
					UserName = "userName",
					Password = "password"
				}));

		}
	}
}
