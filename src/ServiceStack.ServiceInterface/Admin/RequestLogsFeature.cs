using System;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.ServiceInterface.Providers;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface.Admin
{
	public class RequestLogsFeature : IPlugin
	{
		public string AtRestPath { get; set; }

		public int? Capacity { get; set; }

		public IRequestLogger RequestLogger { get; set; }

		public Type[] HideRequestBodyForRequestDtoTypes { get; set; }

		public RequestLogsFeature(int? capacity = null)
		{
			this.AtRestPath = "/requestlogs";
			this.Capacity = capacity;
			this.HideRequestBodyForRequestDtoTypes = new[] {
				typeof(Auth.Auth), typeof(Registration)
			};
		}

		public void Register(IAppHost appHost)
		{
			appHost.RegisterService<RequestLogsService>(AtRestPath);
			appHost.Register(RequestLogger
				?? new InMemoryRollingRequestLogger(Capacity) {
					HideRequestBodyForRequestDtoTypes = HideRequestBodyForRequestDtoTypes
				});
		}
	}
}