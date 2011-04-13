using System.Collections.Generic;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
	public class FunqDepCtor { }
	public class AltDepCtor { }

	public class FunqDepProperty { }
	public class AltDepProperty { }

	public class Ioc { }

	public class IocResponse : IHasResponseStatus
	{
		public IocResponse()
		{
			this.ResponseStatus = new ResponseStatus();
			this.Results = new List<string>();
		}

		public List<string> Results { get; set; }

		public ResponseStatus ResponseStatus { get; set; }
	}

	public class IocService : IService<Ioc>
	{
		private readonly FunqDepCtor funqDepCtor;
		private readonly AltDepCtor altDepCtor;

		public IocService(FunqDepCtor funqDepCtor, AltDepCtor altDepCtor)
		{
			this.funqDepCtor = funqDepCtor;
			this.altDepCtor = altDepCtor;
		}

		public FunqDepProperty FunqDepProperty { get; set; }
		public AltDepProperty AltDepProperty { get; set; }

		public object Execute(Ioc request)
		{
			var response = new IocResponse();

			var deps = new object[] {
				funqDepCtor, altDepCtor, FunqDepProperty, AltDepProperty
			};

			foreach (var dep in deps)
			{
				if (dep != null)
					response.Results.Add(dep.GetType().Name);
			}

			return response;
		}
	}

}