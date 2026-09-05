using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using ServiceStack.Validation;

namespace ServiceStack.Common.Tests.ServiceClient.Web;

[TestFixture]
public class ClientModernizationTests
{
    public class TestDto : IReturn<object>
    {
        public string Prop { get; set; }
        public string Path { get; set; }
    }

    [Route("/files/{Path*}")]
    public class WildcardTestDto : IReturn<object>
    {
        public string Path { get; set; }
    }

    [Test]
    public void GetUrlVariables_Handles_Malformed_Route_Components_Without_Crashing()
    {
        var route1 = new RestRoute(typeof(TestDto), "/test/{/path", "GET", 1);
        Assert.That(route1.IsValid, Is.False);
        Assert.That(route1.ErrorMsg, Does.Contain("Component '{' can not be parsed"));

        var route2 = new RestRoute(typeof(TestDto), "/test/}/path", "GET", 1);
        Assert.That(route2.IsValid, Is.False);
        Assert.That(route2.ErrorMsg, Does.Contain("Component '}' can not be parsed"));

        var route3 = new RestRoute(typeof(TestDto), "/test/{prop}/path", "GET", 1);
        Assert.That(route3.IsValid, Is.True);
        Assert.That(route3.Variables, Does.Contain("prop"));
    }

    [Test]
    public void RestRoute_Apply_Handles_Wildcard_With_Null_Value()
    {
        var request = new WildcardTestDto { Path = null };
        var url = request.ToUrl("GET");
        Assert.That(url, Is.EqualTo("/files/"));
    }

    [Test]
    public void UrlExtensions_Null_Request_Throws_ArgumentNullException()
    {
        object nullDto = null;
        Assert.Throws<ArgumentNullException>(() => nullDto.ToUrl("GET"));
        Assert.Throws<ArgumentNullException>(() => nullDto.ToOneWayUrlOnly());
        Assert.Throws<ArgumentNullException>(() => nullDto.ToOneWayUrl());
        Assert.Throws<ArgumentNullException>(() => nullDto.ToReplyUrlOnly());
        Assert.Throws<ArgumentNullException>(() => nullDto.ToReplyUrl());
    }

    [Test]
    public void UrlExtensions_Null_Type_Returns_Null_Safely()
    {
        Type nullType = null;
        Assert.That(nullType.GetOperationName(), Is.Null);
        Assert.That(nullType.GetFullyQualifiedName(), Is.Null);
        Assert.That(nullType.ExpandTypeName(), Is.Null);
        Assert.That(nullType.ToApiUrl(), Is.Null);
    }

    [Test]
    public void WebServiceException_ToString_Handles_Null_Error_Item_Safely()
    {
        var ex = new WebServiceException("Request failed")
        {
            StatusCode = 400,
            StatusDescription = "Bad Request",
            ResponseStatus = new ResponseStatus
            {
                ErrorCode = "ValidationError",
                Message = "Input is invalid",
                Errors = new List<ResponseError>
                {
                    null,
                    new()
                    {
                        FieldName = "Prop",
                        ErrorCode = "NotEmpty",
                        Message = "Prop is required",
                        Meta = new Dictionary<string, string> { ["Key"] = "Val" }
                    }
                }
            }
        };

        var str = ex.ToString();
        Assert.That(str, Does.Contain("400 Bad Request"));
        Assert.That(str, Does.Contain("[Prop] NotEmpty: Prop is required"));
        Assert.That(str, Does.Contain("Key: Val"));
    }

    [Test]
    public void ResponseStatusUtils_GetDetailedError_Handles_Null_Status_And_Null_Items()
    {
        ResponseStatus nullStatus = null;
        Assert.That(nullStatus.GetDetailedError(), Is.EqualTo(string.Empty));

        var status = new ResponseStatus
        {
            ErrorCode = "Err",
            Message = "Test Error",
            Errors = new List<ResponseError>
            {
                null,
                new() { FieldName = "Field1", ErrorCode = "Required", Message = "Missing" }
            },
            StackTrace = "at SomeMethod()"
        };

        var detailed = status.GetDetailedError();
        Assert.That(detailed, Does.Contain("Err Test Error"));
        Assert.That(detailed, Does.Contain("- Field1: Required Missing"));
        Assert.That(detailed, Does.Contain("StackTrace:"));
    }

    [Test]
    public void ResponseStatusUtils_CreateResponseStatus_Throws_Expected_Grammar_Message()
    {
        var ex = Assert.Throws<ArgumentException>(() => ResponseStatusUtils.CreateResponseStatus(null, null));
        Assert.That(ex.Message, Does.Contain("with an empty errorCode"));
    }

    [Test]
    public void WebRequestUtils_AuthenticationInfo_Throws_On_Null_Or_Empty()
    {
        Assert.Throws<ArgumentNullException>(() => new AuthenticationInfo(null));
        Assert.Throws<ArgumentNullException>(() => new AuthenticationInfo(""));
    }

    [Test]
    public void ClientDiagnosticUtils_InitMessage_Handles_Null_Message()
    {
        var listener = new DiagnosticListener("ServiceStack.Tests");
        Assert.DoesNotThrow(() => listener.InitMessage(null));
    }
}
