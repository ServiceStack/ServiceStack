using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using ServiceStack.Common.Extensions;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
	[DataContract]
	[Route("/errors")]
	[Route("/errors/{Type}")]
	[Route("/errors/{Type}/{StatusCode}")]
	[Route("/errors/{Type}/{StatusCode}/{Message}")]
	public class HttpError
	{
		[DataMember]
		public string Type { get; set; }

		[DataMember]
		public string Message { get; set; }

		[DataMember]
		public int? StatusCode { get; set; }
	}

	[DataContract]
	public class HttpErrorResponse
		: IHasResponseStatus
	{
		public HttpErrorResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}

	public class HttpErrorService : ServiceInterface.Service
	{
	    public object Any(HttpError request)
		{
			if (request.Type.IsNullOrEmpty())
				throw new ArgumentNullException("Type");

			var ex = new Exception(request.Message);
			switch (request.Type)
			{
				case "FileNotFoundException":
					ex = new FileNotFoundException(request.Message);
					break;
			}

			if (!request.StatusCode.HasValue)
				throw ex;

			var httpStatus = (HttpStatusCode)request.StatusCode.Value;
			throw new Common.Web.HttpError(httpStatus, ex);
		}
	}

}