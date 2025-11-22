using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicAppointmentSystem.Models;
using ClinicAppointmentSystem.Data;

namespace ClinicAppointmentSystem.Controllers
{
    [Authorize(Roles = "Client")]
    public class ClientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClientController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var today = DateTime.Today;

            var dashboardStats = new
            {
                TotalAppointments = await _context.Appointments
                    .CountAsync(a => a.PatientId == user.Id),
                UpcomingAppointments = await _context.Appointments
                    .CountAsync(a => a.PatientId == user.Id && a.AppointmentDate >= today && a.Status == "Confirmed"),
                PendingAppointments = await _context.Appointments
                    .CountAsync(a => a.PatientId == user.Id && a.Status == "Pending"),
                CompletedAppointments = await _context.Appointments
                    .CountAsync(a => a.PatientId == user.Id && a.Status == "Completed")
            };

            ViewBag.DashboardStats = dashboardStats;

            // Upcoming appointments
            var upcomingAppointments = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Service)
                .Where(a => a.PatientId == user.Id && a.AppointmentDate >= today && a.Status != "Cancelled")
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.StartTime)
                .Take(5)
                .ToListAsync();

            return View(upcomingAppointments);
        }

        public async Task<IActionResult> MyAppointments()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Service)
                .Where(a => a.PatientId == user.Id)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.StartTime)
                .ToListAsync();

            return View(appointments);
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ApplicationUser model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Profile updated successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error updating profile.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "User not found.";
            }

            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int appointmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.PatientId == user.Id);

            if (appointment != null && appointment.Status != "Completed" && appointment.Status != "Cancelled")
            {
                // Check if appointment is at least 2 hours away
                var appointmentDateTime = appointment.AppointmentDate.Add(appointment.StartTime);
                if (appointmentDateTime > DateTime.Now.AddHours(2))
                {
                    appointment.Status = "Cancelled";
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Appointment cancelled successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Appointments can only be cancelled at least 2 hours in advance.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Appointment not found or cannot be cancelled.";
            }

            return RedirectToAction(nameof(MyAppointments));
        }
    }
}