using Microsoft.AspNetCore.Mvc;
using ServiceStack.Mvc;

namespace Mvc.Core.Tests.Controllers
{
    public class TestController : ServiceStackController
    {
        [RedirectTestFilter("http://google.com")]
        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}