using System;
using System.Runtime.Serialization;
using ServiceStack.Web;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	[DataContract]
	[Route("/VerbMatch", "POST,PUT,PATCH")]
	[Route("/VerbMatch/{Name}", "POST,PUT,PATCH")]
	public class VerbMatch2
	{
		[DataMember]
		public string Name { get; set; }
	}

	[DataContract]
	public class VerbMatch2Response
	{
		[DataMember]
		public string Result { get; set; }
	}

	public class VerbMatch2Service : IService
	{
		public object Any(VerbMatch2 request)
		{
			throw new NotImplementedException();
		}

		public object Get(VerbMatch2 request)
		{
			return new VerbMatch2Response { Result = HttpMethods.Get };
		}

		public object Post(VerbMatch2 request)
		{
			return new VerbMatch2Response { Result = HttpMethods.Post };
		}

		public object Put(VerbMatch2 request)
		{
			return new VerbMatch2Response { Result = HttpMethods.Put };
		}

		public object Delete(VerbMatch2 request)
		{
			return new VerbMatch2Response { Result = HttpMethods.Delete };
		}

		public object Patch(VerbMatch2 request)
		{
			return new VerbMatch2Response { Result = HttpMethods.Patch };
		}
	}

}