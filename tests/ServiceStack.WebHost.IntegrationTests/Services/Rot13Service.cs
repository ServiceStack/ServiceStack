using System;
using ServiceStack.ServiceInterface;
using ServiceStack.Text;
using ServiceStack.WebHost.IntegrationTests.Operations;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	public class Rot13Service 
		: ServiceBase<Rot13>
	{
		protected override object Run(Rot13 request)
		{
			return new Rot13Response { Result = request.Value.ToRot13() };
		}
	}
}