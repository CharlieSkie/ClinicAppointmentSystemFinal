using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicAppointmentSystem.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Dashboard", "Admin");
                else if (User.IsInRole("Staff"))
                    return RedirectToAction("Dashboard", "Staff");
                else if (User.IsInRole("Client"))
                    return RedirectToAction("Dashboard", "Client");
            }
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AdminDashboard()
        {
            return RedirectToAction("Dashboard", "Admin");
        }

        [Authorize(Roles = "Staff")]
        public IActionResult StaffDashboard()
        {
            return RedirectToAction("Dashboard", "Staff");
        }

        [Authorize(Roles = "Client")]
        public IActionResult ClientDashboard()
        {
            return RedirectToAction("Dashboard", "Client");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}