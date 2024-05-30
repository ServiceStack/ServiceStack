using System.Collections.Generic;
using System.Runtime.Serialization;
using NUnit.Framework;
#if NETFRAMEWORK
using ServiceStack.ServiceModel;
#endif

namespace ServiceStack.WebHost.Endpoints.Tests;

[DataContract]
public class WithStatusResponse
{
	[DataMember]
	public ResponseStatus ResponseStatus { get; set; }
}

[DataContract]
public class NoStatusResponse
{
}

[TestFixture]
public class WebServiceExceptionTests
{
	[Test]
	public void Can_retrieve_Errors_from_Dto_WithStatusResponse()
	{
		var webEx = new WebServiceException
		{
			ResponseDto = new WithStatusResponse
			{
				ResponseStatus = new ResponseStatus
				{
					ErrorCode = "errorCode",
					Message = "errorMessage",
					StackTrace = "stackTrace"
				}
			}
		};

		Assert.That(webEx.ErrorCode, Is.EqualTo("errorCode"));
		Assert.That(webEx.ErrorMessage, Is.EqualTo("errorMessage"));
		Assert.That(webEx.ServerStackTrace, Is.EqualTo("stackTrace"));
	}

	[Test]
	public void Can_Retrieve_Errors_From_ResponseBody_If_ResponseDto_Does_Not_Contain_ResponseStatus()
	{
		var webEx = new WebServiceException {
			ResponseDto = new List<string> {"123"},
			ResponseBody = "{\"ResponseStatus\":" +
			               "{\"ErrorCode\":\"UnauthorizedAccessException\"," +
			               "\"Message\":\"Error Message\"," +
			               "\"StackTrace\":\"Some Stack Trace\",\"Errors\":[]}}",
		};

		Assert.That(webEx.ErrorCode, Is.EqualTo("UnauthorizedAccessException"));
		Assert.That(webEx.ErrorMessage, Is.EqualTo("Error Message"));
		Assert.That(webEx.ServerStackTrace, Is.EqualTo("Some Stack Trace"));
	}
}