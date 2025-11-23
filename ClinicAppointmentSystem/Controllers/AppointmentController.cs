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
            ViewBag.Doctors = doctors;

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Client")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(
            int doctorId,
            int serviceId,
            DateTime appointmentDate,
            TimeSpan startTime,
            TimeSpan endTime,
            string? notes = null)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                if (user == null)
                {
                    return Challenge();
                }

                // Manual validation
                if (doctorId == 0 || serviceId == 0)
                {
                    TempData["ErrorMessage"] = "Please fill all required fields.";
                    return await RedirectToBook();
                }

                // Check if patient already has an appointment on the same day
                if (!await _appointmentService.CanPatientBookAppointmentAsync(user.Id, appointmentDate))
                {
                    TempData["ErrorMessage"] = "You already have an appointment scheduled for this day. Only one appointment per day is allowed.";
                    return await RedirectToBook();
                }

                // Check if the selected time slot is available
                var bookedSlots = await _appointmentService.GetBookedSlotsCountAsync(
                    doctorId, appointmentDate, startTime, endTime);

                if (bookedSlots > 0)
                {
                    TempData["ErrorMessage"] = "The selected time slot is no longer available. Please choose a different time.";
                    return await RedirectToBook();
                }

                var appointment = new Appointment
                {
                    PatientId = user.Id,
                    DoctorId = doctorId,
                    ServiceId = serviceId,
                    AppointmentDate = appointmentDate,
                    StartTime = startTime,
                    EndTime = endTime,
                    Notes = notes,
                    Status = "Pending",
                    AppointmentId = await _appointmentService.GenerateAppointmentIdAsync(),
                    CreatedDate = DateTime.UtcNow
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Appointment booked successfully! Your appointment ID is: " + appointment.AppointmentId;
                return RedirectToAction("MyAppointments", "Client");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error booking appointment: {ex.Message}";
                return await RedirectToBook();
            }
        }

        private async Task<IActionResult> RedirectToBook()
        {
            var doctors = await _context.Doctors.Where(d => d.IsActive).ToListAsync();
            ViewBag.Doctors = doctors;
            return View("Book");
        }

        [HttpGet]
        public async Task<JsonResult> GetAvailableTimeSlots(int doctorId, DateTime date)
        {
            var availableSlots = await _appointmentService.GetAvailableTimeSlotsAsync(doctorId, date);
            return Json(availableSlots);
        }
    }
}