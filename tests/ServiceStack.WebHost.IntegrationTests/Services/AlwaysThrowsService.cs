using System;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.IntegrationTests.Operations;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	public class AlwaysThrowsService 
		: ServiceBase<AlwaysThrows>
	{
		protected override object Run(AlwaysThrows request)
		{
			throw new NotImplementedException(GetErrorMessage(request.Value));
		}

		public static string GetErrorMessage(string value)
		{
			return value + " is not implemented";
		}
	}
}