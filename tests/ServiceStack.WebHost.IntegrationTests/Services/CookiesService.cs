using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
    [Route("/cookies")]
    public class Cookies : IReturn<CookiesResponse> {}

    public class CookiesResponse
    {
        public List<string> RequestCookieNames { get; set; }
    }

    public class CookiesService : Service
    {
        public CookiesResponse Any(Cookies c)
        {
            var response = new CookiesResponse
            {
                RequestCookieNames = Request.Cookies.Keys.ToList(),
            };
            return response;
        }
    }
}