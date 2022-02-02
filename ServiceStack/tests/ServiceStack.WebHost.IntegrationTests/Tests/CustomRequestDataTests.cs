using System.IO;
using System.Net;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [TestFixture]
    public class CustomRequestDataTests
    {
        /// <summary>
        /// first-name=tom&item-0=blah&item-1-delete=1
        /// </summary>
        [Test]
        public void Can_parse_custom_form_data()
        {
            var webReq = (HttpWebRequest)WebRequest.Create(Config.ServiceStackBaseUri + "/customformdata?format=json");
            webReq.Method = HttpMethods.Post;
            webReq.ContentType = MimeTypes.FormUrlEncoded;

            try
            {
                using (var sw = new StreamWriter(webReq.GetRequestStream()))
                {
                    sw.Write("&first-name=tom&item-0=blah&item-1-delete=1");
                }
                var response = webReq.GetResponse().ReadToEnd();

                Assert.That(response, Is.EqualTo("{\"firstName\":\"tom\",\"item0\":\"blah\",\"item1Delete\":\"1\"}"));
            }
            catch (WebException webEx)
            {
                var errorWebResponse = ((HttpWebResponse)webEx.Response);
                var errorResponse = errorWebResponse.ReadToEnd();

                Assert.Fail(errorResponse);
            }
        }

    }
}