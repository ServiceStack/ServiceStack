using System.Web.Mvc;
using ServiceStack.Mvc;

namespace CheckMvc.Controllers
{
    public class HomeViewModel
    {
        public string Name { get; set; }
        public long Counter { get; set; }
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
                Counter = Redis.IncrementValue("counter:" + name)
            };
        }

        public ActionResult About()
        {
            return View(GetViewModel("About"));
        }

        public ActionResult Contact()
        {
            return View(GetViewModel("Contact"));
        }
    }
}