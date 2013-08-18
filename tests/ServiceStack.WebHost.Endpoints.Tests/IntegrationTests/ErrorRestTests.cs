using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.WebHost.Endpoints.Tests.IntegrationTests
{
	[TestFixture]
	public class ErrorRestTests : IntegrationTestBase
	{
		[Test]
		public void ReproduceErrorTest()
		{
			var restClient = new JsonServiceClient(BaseUrl);

			var errorList = restClient.Get<ErrorCollectionResponse>("error");
			Assert.That(errorList.Result.Count, Is.EqualTo(1));

			var error = restClient.Post<ErrorResponse>("error", new Error { Id = "Test" });
			Assert.That(error, !Is.Null);
		}

		[Test]
		public void UseSameRestClientError()
		{
			var restClient = new JsonServiceClient(BaseUrl);
			var errorList = restClient.Get<ErrorCollectionResponse>("error");
			Assert.That(errorList.Result.Count, Is.EqualTo(1));

			var error = restClient.Get<ErrorResponse>("error/Test");
			Assert.That(error, !Is.Null);
		}
	}

	[Route("/error")]
	[Route("/error/{Id}")]
	public class Error
	{
		public Error()
		{
		}

		public string Id { get; set; }
		public Error Inner { get; set; }
	}

	public class ErrorService : ServiceInterface.Service
	{
		public object Get(Error request)
		{
			if (request != null && !String.IsNullOrEmpty(request.Id))
				return new ErrorResponse(new Error { Id = "Test" });

			return new ErrorCollectionResponse(new List<Error> { new Error { Id = "TestCollection" } });
		}

		public object Post(Error request)
		{
			return new ErrorResponse(request);
		}
	}

	public class ErrorResponse : IHasResponseStatus
	{
		public ErrorResponse(Error result)
		{
			Result = result;
			ResponseStatus = new ResponseStatus();
		}

		public Error Result { get; set; }

		public ResponseStatus ResponseStatus { get; set; }
	}

	public class ErrorCollectionResponse : IHasResponseStatus
	{
		public ErrorCollectionResponse(IList<Error> result)
		{
			Result = new Collection<Error>(result);
			ResponseStatus = new ResponseStatus();
		}

		public Collection<Error> Result { get; set; }

		public ResponseStatus ResponseStatus { get; set; }
	}

}