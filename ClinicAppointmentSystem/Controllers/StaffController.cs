using ClinicAppointmentSystem.Data;
using ClinicAppointmentSystem.Models;
using ClinicAppointmentSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointmentSystem.Controllers
{
    [Authorize(Roles = "Staff")]
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAppointmentService _appointmentService;

        public StaffController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAppointmentService appointmentService)
        {
            _context = context;
            _userManager = userManager;
            _appointmentService = appointmentService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var today = DateTime.Today;
            var dashboardStats = new
            {
                TodayAppointments = await _context.Appointments
                    .CountAsync(a => a.AppointmentDate.Date == today),
                PendingAppointments = await _context.Appointments
                    .CountAsync(a => a.Status == "Pending"),
                TotalPatients = await _context.Users
                    .CountAsync(u => u.Role == "Client" && u.IsApproved && u.IsActive),
                UpcomingAppointments = await _context.Appointments
                    .CountAsync(a => a.AppointmentDate >= today && a.Status == "Confirmed")
            };

            ViewBag.DashboardStats = dashboardStats;

            // Today's appointments
            var todayAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Service)
                .Where(a => a.AppointmentDate.Date == today)
                .OrderBy(a => a.StartTime)
                .ToListAsync();

            return View(todayAppointments);
        }

        public async Task<IActionResult> Appointments()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Service)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenBy(a => a.StartTime)
                .ToListAsync();
            return View(appointments);
        }

        public async Task<IActionResult> CreateAppointment()
        {
            ViewBag.Doctors = await _context.Doctors.Where(d => d.IsActive).ToListAsync();
            ViewBag.Services = await _context.Services.Where(s => s.IsActive).ToListAsync();
            ViewBag.Patients = await _context.Users
                .Where(u => u.Role == "Client" && u.IsApproved && u.IsActive)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAppointment(Appointment appointment)
        {
            if (ModelState.IsValid)
            {
                // For staff-created appointments, auto-confirm
                appointment.Status = "Confirmed";
                appointment.AppointmentId = await _appointmentService.GenerateAppointmentIdAsync();
                appointment.CreatedDate = DateTime.UtcNow;

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Appointment created successfully! Appointment ID: {appointment.AppointmentId}";
                return RedirectToAction(nameof(Appointments));
            }

            ViewBag.Doctors = await _context.Doctors.Where(d => d.IsActive).ToListAsync();
            ViewBag.Services = await _context.Services.Where(s => s.IsActive).ToListAsync();
            ViewBag.Patients = await _context.Users
                .Where(u => u.Role == "Client" && u.IsApproved && u.IsActive)
                .ToListAsync();

            return View(appointment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAppointmentStatus(int appointmentId, string status)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment != null)
            {
                appointment.Status = status;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Appointment status updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Appointment not found.";
            }
            return RedirectToAction(nameof(Appointments));
        }

        public async Task<IActionResult> Patients()
        {
            var patients = await _context.Users
                .Where(u => u.Role == "Client" && u.IsApproved && u.IsActive)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();
            return View(patients);
        }

        public async Task<IActionResult> Schedule()
        {
            var schedules = await _context.Schedules
                .Include(s => s.Doctor)
                .Where(s => s.IsActive)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
            return View(schedules);
        }
    }
}