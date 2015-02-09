using System;
using Check.ServiceModel;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Text;

namespace CheckHttpListener
{
    public class CustomMethodRoutesTests
    {
        [Test]
        public void Can_generate_CustomMethod()
        {
            var requestDto = new GetOrganizationRequest
            {
                Id = new Guid("ca61162b0c30491d8d91e74230f23a66"),
                IncludeAddresses = true
            };

            ServiceClientBase.GlobalRequestFilter = httpReq =>
            {
                httpReq.RequestUri.ToString().Print();
                Assert.That(httpReq.RequestUri.ToString(),
                    Is.EqualTo("http://www.google.com/organizations/ca61162b0c30491d8d91e74230f23a66?includeAddresses=True"));
            };

            var client = new JsonServiceClient("http://www.google.com/");

            try
            {
                client.CustomMethod("GET", requestDto);
            }
            catch (WebServiceException) { }

            try
            {
                client.CustomMethodAsync("GET", requestDto).Wait();
            }
            catch (AggregateException aex)
            {
                if (!(aex.UnwrapIfSingleException() is WebServiceException))
                    throw;
            }

            try
            {
                client.Get(requestDto);
            }
            catch (WebServiceException) { }
        }
    }
}