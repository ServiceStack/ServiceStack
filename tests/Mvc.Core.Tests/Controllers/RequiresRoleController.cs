using Microsoft.AspNetCore.Mvc;
using ServiceStack;
using ServiceStack.Mvc;

namespace Mvc.Core.Tests.Controllers
{
    [RequiredRole("TheRole")]
    public class RequiresRoleController : ServiceStackController
    {
        public ActionResult Index()
        {
            var session = SessionAs<CustomUserSession>();
            return View(session);
        }
    }
}