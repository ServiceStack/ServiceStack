using System;
using System.Runtime.Serialization;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	[DataContract]
	[Route("/VerbMatch", "GET,DELETE")]
	[Route("/VerbMatch/{Name}", "GET,DELETE")]
	public class VerbMatch1
	{
		[DataMember]
		public string Name { get; set; }
	}

	[DataContract]
	public class VerbMatch1Response
	{
		[DataMember]
		public string Result { get; set; }
	}

	public class VerbMatch1Service : IService<VerbMatch1>, IRestService<VerbMatch1>
	{
		public object Execute(VerbMatch1 request)
		{
			throw new NotImplementedException();
		}

		public object Get(VerbMatch1 request)
		{
			return new VerbMatch1Response { Result = HttpMethods.Get };
		}

		public object Post(VerbMatch1 request)
		{
			return new VerbMatch1Response { Result = HttpMethods.Post };
		}

		public object Put(VerbMatch1 request)
		{
			return new VerbMatch1Response { Result = HttpMethods.Put };
		}

		public object Delete(VerbMatch1 request)
		{
			return new VerbMatch1Response { Result = HttpMethods.Delete };
		}

		public object Patch(VerbMatch1 request)
		{
			return new VerbMatch1Response { Result = HttpMethods.Patch };
		}
	}

}