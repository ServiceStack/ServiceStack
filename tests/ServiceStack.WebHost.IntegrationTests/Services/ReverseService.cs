using System;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.IntegrationTests.Operations;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	public class ReverseService 
		: ServiceBase<Reverse>
	{
		protected override object Run(Reverse request)
		{
			return new ReverseResponse { Result = Execute(request.Value) };
		}

		public static string Execute(string value)
		{
			var valueBytes = value.ToCharArray();
			Array.Reverse(valueBytes);
			return new string(valueBytes);
		}
	}

}