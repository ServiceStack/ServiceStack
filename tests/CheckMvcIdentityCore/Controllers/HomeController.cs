using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using IdentityDemo.Models;
using Microsoft.AspNetCore.Authorization;

namespace IdentityDemo.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Anyone can access this page.";

            return View();
        }

        [Authorize]
        public IActionResult About()
        {
            ViewData["Message"] = "Access limited to any Authenticated User.";

            return View();
        }

        [Authorize(Roles = "Member")]
        public IActionResult Member()
        {
            ViewData["Message"] = "Access limited to users with 'Member' role.";

            return View();
        }

        [Authorize(Roles = "Manager")]
        public IActionResult Manager()
        {
            ViewData["Message"] = "Access limited to users with 'Manager' role.";

            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Admin()
        {
            ViewData["Message"] = "Access limited to users with 'Admin' role.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
