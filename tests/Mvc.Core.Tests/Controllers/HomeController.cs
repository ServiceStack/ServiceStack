using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Mvc;
using ServiceStack.OrmLite;

namespace Mvc.Core.Tests.Controllers
{
    public class RedirectTestFilterAttribute : ActionFilterAttribute
    {
        private readonly string url;

        public RedirectTestFilterAttribute(string url)
        {
            this.url = url;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            context.Result = new RedirectResult(url);
        }
    }

    public class HomeController : ServiceStackController
    {
        public HomeViewModel GetViewModel()
        {
            var response = new HomeViewModel { Session = SessionAs<CustomUserSession>() };
            if (response.Session.UserAuthId != null)
            {
                var userAuthId = int.Parse(response.Session.UserAuthId);
                response.UserAuths = Db.Select<UserAuth>(x => x.Id == userAuthId);
                response.UserAuthDetails = Db.Select<UserAuthDetails>(x => x.UserAuthId == userAuthId);
            }
            return response;
        }

        public ActionResult Index()
        {
            return View(GetViewModel());
        }

        public ActionResult Login(string userName, string password, string redirect = null)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var authService = ResolveService<AuthenticateService>())
                    {
                        var response = authService.Authenticate(new Authenticate
                        {
                            provider = CredentialsAuthProvider.Name,
                            UserName = userName,
                            Password = password,
                            RememberMe = true,
                        });

                        if (!string.IsNullOrEmpty(redirect))
                            return Redirect(redirect);
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return View("Index", GetViewModel());
        }

        public ActionResult Logout()
        {
            using (var authService = ResolveService<AuthenticateService>())
            {
                authService.Authenticate(new Authenticate { provider = "logout" });
            }

            return View("Index", GetViewModel());
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }
        
        [RedirectTestFilter("http://google.com")]
        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}
