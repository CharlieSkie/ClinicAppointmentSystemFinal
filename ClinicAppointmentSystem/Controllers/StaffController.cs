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
            ViewBag.Patients = await _context.Users
                .Where(u => u.Role == "Client" && u.IsApproved && u.IsActive)
                .ToListAsync();
            ViewBag.Services = await _context.Services.Where(s => s.IsActive).ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAppointment(
            string patientId,
            int doctorId,
            int serviceId,
            DateTime appointmentDate,
            TimeSpan startTime,
            TimeSpan endTime,
            string? notes = null)
        {
            try
            {
                // Manual validation
                if (string.IsNullOrEmpty(patientId) || doctorId == 0 || serviceId == 0)
                {
                    TempData["ErrorMessage"] = "Please fill all required fields.";
                    return await RedirectToCreateAppointment();
                }

                var appointment = new Appointment
                {
                    PatientId = patientId,
                    DoctorId = doctorId,
                    ServiceId = serviceId,
                    AppointmentDate = appointmentDate,
                    StartTime = startTime,
                    EndTime = endTime,
                    Notes = notes,
                    Status = "Confirmed",
                    AppointmentId = await _appointmentService.GenerateAppointmentIdAsync(),
                    CreatedDate = DateTime.UtcNow
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Appointment created successfully! Appointment ID: {appointment.AppointmentId}";
                return RedirectToAction(nameof(Appointments));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating appointment: {ex.Message}";
                return await RedirectToCreateAppointment();
            }
        }

        private async Task<IActionResult> RedirectToCreateAppointment()
        {
            ViewBag.Doctors = await _context.Doctors.Where(d => d.IsActive).ToListAsync();
            ViewBag.Patients = await _context.Users
                .Where(u => u.Role == "Client" && u.IsApproved && u.IsActive)
                .ToListAsync();
            ViewBag.Services = await _context.Services.Where(s => s.IsActive).ToListAsync();
            return View("CreateAppointment");
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
                .OrderBy(s => s.Doctor.Name)
                .ThenBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
            return View(schedules);
        }

        public async Task<IActionResult> CreateSchedule()
        {
            ViewBag.Doctors = await _context.Doctors.Where(d => d.IsActive).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSchedule(
            int doctorId,
            int dayOfWeek,
            TimeSpan startTime,
            TimeSpan endTime,
            int maxAppointments,
            bool isActive = true)
        {
            try
            {
                // Manual validation
                if (doctorId == 0)
                {
                    TempData["ErrorMessage"] = "Please select a doctor.";
                    return await RedirectToCreateSchedule();
                }

                var schedule = new Schedule
                {
                    DoctorId = doctorId,
                    DayOfWeek = (DayOfWeek)dayOfWeek,
                    StartTime = startTime,
                    EndTime = endTime,
                    MaxAppointments = maxAppointments,
                    IsActive = isActive
                };

                _context.Schedules.Add(schedule);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Schedule created successfully.";
                return RedirectToAction(nameof(Schedule));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating schedule: {ex.Message}";
                return await RedirectToCreateSchedule();
            }
        }

        private async Task<IActionResult> RedirectToCreateSchedule()
        {
            ViewBag.Doctors = await _context.Doctors.Where(d => d.IsActive).ToListAsync();
            return View("CreateSchedule");
        }

        public async Task<IActionResult> EditSchedule(int id)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Doctor)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (schedule == null)
            {
                return NotFound();
            }

            ViewBag.Doctors = await _context.Doctors.Where(d => d.IsActive).ToListAsync();
            return View(schedule);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSchedule(int id, Schedule schedule)
        {
            if (id != schedule.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(schedule);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Schedule updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ScheduleExists(schedule.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Schedule));
            }

            ViewBag.Doctors = await _context.Doctors.Where(d => d.IsActive).ToListAsync();
            return View(schedule);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule != null)
            {
                _context.Schedules.Remove(schedule);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Schedule deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Schedule not found.";
            }
            return RedirectToAction(nameof(Schedule));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleScheduleStatus(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule != null)
            {
                schedule.IsActive = !schedule.IsActive;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Schedule {(schedule.IsActive ? "activated" : "deactivated")} successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Schedule not found.";
            }
            return RedirectToAction(nameof(Schedule));
        }

        private bool ScheduleExists(int id)
        {
            return _context.Schedules.Any(e => e.Id == id);
        }
    }
}