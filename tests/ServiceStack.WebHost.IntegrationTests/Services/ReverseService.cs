using System;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.IntegrationTests.Operations;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	public class ReverseService : ServiceBase<Reverse>
	{
		protected override object Run(Reverse request)
		{
			var valueBytes = request.Value.ToCharArray();
			Array.Reverse(valueBytes);
			return new ReverseResponse { Result = new string(valueBytes) };
		}
	}
}