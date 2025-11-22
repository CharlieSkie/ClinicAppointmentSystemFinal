using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicAppointmentSystem.Models;
using ClinicAppointmentSystem.Data;
using ClinicAppointmentSystem.Services;

namespace ClinicAppointmentSystem.Controllers
{
    [Authorize]
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAppointmentService _appointmentService;

        public AppointmentController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAppointmentService appointmentService)
        {
            _context = context;
            _userManager = userManager;
            _appointmentService = appointmentService;
        }

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Book()
        {
            var doctors = await _context.Doctors.Where(d => d.IsActive).ToListAsync();
            var services = await _context.Services.Where(s => s.IsActive).ToListAsync();

            ViewBag.Doctors = doctors;
            ViewBag.Services = services;

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Client")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(Appointment appointment)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            // Check if patient already has an appointment on the same day
            if (!await _appointmentService.CanPatientBookAppointmentAsync(user.Id, appointment.AppointmentDate))
            {
                ModelState.AddModelError("", "You already have an appointment scheduled for this day. Only one appointment per day is allowed.");
            }

            // Check if the selected time slot is available
            var bookedSlots = await _appointmentService.GetBookedSlotsCountAsync(
                appointment.DoctorId, appointment.AppointmentDate, appointment.StartTime, appointment.EndTime);

            if (bookedSlots > 0)
            {
                ModelState.AddModelError("", "The selected time slot is no longer available. Please choose a different time.");
            }

            if (ModelState.IsValid)
            {
                appointment.PatientId = user.Id;
                appointment.AppointmentId = await _appointmentService.GenerateAppointmentIdAsync();
                appointment.Status = "Pending";
                appointment.CreatedDate = DateTime.UtcNow;

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Appointment booked successfully! Your appointment ID is: " + appointment.AppointmentId;
                return RedirectToAction("MyAppointments", "Client");
            }

            var doctors = await _context.Doctors.Where(d => d.IsActive).ToListAsync();
            var services = await _context.Services.Where(s => s.IsActive).ToListAsync();

            ViewBag.Doctors = doctors;
            ViewBag.Services = services;

            return View(appointment);
        }

        [HttpGet]
        public async Task<JsonResult> GetAvailableTimeSlots(int doctorId, DateTime date)
        {
            var availableSlots = await _appointmentService.GetAvailableTimeSlotsAsync(doctorId, date);
            return Json(availableSlots);
        }

        [HttpGet]
        public async Task<JsonResult> GetServiceDuration(int serviceId)
        {
            var service = await _context.Services.FindAsync(serviceId);
            if (service != null)
            {
                return Json(service.DurationMinutes);
            }
            return Json(30); // Default duration
        }
    }
}