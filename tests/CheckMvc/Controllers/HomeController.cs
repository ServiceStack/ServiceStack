using System;
using System.Web.Mvc;
using ServiceStack;
using ServiceStack.Mvc;

namespace CheckMvc.Controllers
{
    public class HomeViewModel
    {
        public string Name { get; set; }
        public long Counter { get; set; }
        public string Json { get; set; }
    }

    public class TestObj
    {
        public int Id { get; set; }
        public DateTime CurrentDate { get; set; }
    }

    public class HomeController : ServiceStackController
    {
        public ActionResult Index()
        {
            return View(GetViewModel("Index"));
        }

        private HomeViewModel GetViewModel(string name)
        {
            return new HomeViewModel
            {
                Name = name,
                Counter = Redis.IncrementValue("counter:" + name),
                Json = new TestObj
                {
                    Id = 1,
                    CurrentDate = DateTime.Now
                }.ToJson()
            };
        }

        public ActionResult About()
        {
            return View(GetViewModel("About"));
        }

        public ActionResult Contact()
        {
            try
            {
                var request = new TestGateway {Name = "MVC"};
                var gateway = HostContext.AppHost.GetServiceGateway(HostContext.GetCurrentRequest());
                var response = gateway.Send(request);
                //within above call the validator throws exception
            }
            catch (WebServiceException ex)
            {
                // no longer reaches here
                if (ex.ResponseStatus.ErrorCode == "NotFound")
                    return HttpNotFound();
                throw;
            }
            
            return View(GetViewModel("Contact"));
        }
    }
}