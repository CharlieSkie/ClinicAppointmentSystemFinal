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

            // Use separate async calls instead of complex anonymous type
            var todayAppointmentsCount = await _context.Appointments
                .CountAsync(a => a.AppointmentDate.Date == today);
            var pendingAppointmentsCount = await _context.Appointments
                .CountAsync(a => a.Status == "Pending");
            var totalPatientsCount = await _context.Users
                .CountAsync(u => u.Role == "Client" && u.IsApproved && u.IsActive);
            var upcomingAppointmentsCount = await _context.Appointments
                .CountAsync(a => a.AppointmentDate >= today && a.Status == "Confirmed");

            var dashboardStats = new
            {
                TodayAppointments = todayAppointmentsCount,
                PendingAppointments = pendingAppointmentsCount,
                TotalPatients = totalPatientsCount,
                UpcomingAppointments = upcomingAppointmentsCount
            };

            ViewBag.DashboardStats = dashboardStats;

            var todayAppointments = await GetTodayAppointmentsAsync(today);
            return View(todayAppointments);
        }

        public async Task<IActionResult> Appointments()
        {
            var appointments = await GetAllAppointmentsAsync();
            return View(appointments);
        }

        public async Task<IActionResult> CreateAppointment()
        {
            await LoadCreateAppointmentViewDataAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAppointment(
            string patientId,
            int doctorId,
            DateTime appointmentDate,
            TimeSpan startTime,
            TimeSpan endTime,
            string? notes = null)
        {
            try
            {
                // Manual validation
                if (string.IsNullOrEmpty(patientId) || doctorId == 0)
                {
                    TempData["ErrorMessage"] = "Please fill all required fields.";
                    await LoadCreateAppointmentViewDataAsync();
                    return View("CreateAppointment");
                }

                var appointmentId = await _appointmentService.GenerateAppointmentIdAsync();
                var appointment = new Appointment
                {
                    PatientId = patientId,
                    DoctorId = doctorId,
                    ServiceId = 1, // Default service ID
                    AppointmentDate = appointmentDate,
                    StartTime = startTime,
                    EndTime = endTime,
                    Notes = notes,
                    Status = "Confirmed",
                    AppointmentId = appointmentId,
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
                await LoadCreateAppointmentViewDataAsync();
                return View("CreateAppointment");
            }
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
            var patients = await GetActivePatientsAsync();
            return View(patients);
        }

        public async Task<IActionResult> Schedule()
        {
            var schedules = await GetActiveSchedulesAsync();
            return View(schedules);
        }

        public async Task<IActionResult> CreateSchedule()
        {
            await LoadCreateScheduleViewDataAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSchedule(
            int doctorId,
            int dayOfWeek,
            TimeSpan startTime,
            TimeSpan endTime,
            int maxAppointments)
        {
            try
            {
                // Manual validation
                if (doctorId == 0)
                {
                    TempData["ErrorMessage"] = "Please select a doctor.";
                    await LoadCreateScheduleViewDataAsync();
                    return View("CreateSchedule");
                }

                var schedule = new Schedule
                {
                    DoctorId = doctorId,
                    DayOfWeek = (DayOfWeek)dayOfWeek,
                    StartTime = startTime,
                    EndTime = endTime,
                    MaxAppointments = maxAppointments,
                    IsActive = true // Always active when created
                };

                _context.Schedules.Add(schedule);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Schedule created successfully.";
                return RedirectToAction(nameof(Schedule));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating schedule: {ex.Message}";
                await LoadCreateScheduleViewDataAsync();
                return View("CreateSchedule");
            }
        }

        public async Task<IActionResult> EditSchedule(int id)
        {
            var schedule = await GetScheduleByIdAsync(id);
            if (schedule == null)
            {
                return NotFound();
            }

            await LoadCreateScheduleViewDataAsync();
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
                    // Ensure schedule remains active when editing
                    schedule.IsActive = true;
                    _context.Update(schedule);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Schedule updated successfully.";
                    return RedirectToAction(nameof(Schedule));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await ScheduleExistsAsync(schedule.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            await LoadCreateScheduleViewDataAsync();
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

        // Helper methods to break down complex async operations
        private async Task<List<Appointment>> GetTodayAppointmentsAsync(DateTime today)
        {
            return await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Service)
                .Where(a => a.AppointmentDate.Date == today)
                .OrderBy(a => a.StartTime)
                .ToListAsync();
        }

        private async Task<List<Appointment>> GetAllAppointmentsAsync()
        {
            return await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Service)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenBy(a => a.StartTime)
                .ToListAsync();
        }

        private async Task LoadCreateAppointmentViewDataAsync()
        {
            ViewBag.Doctors = await GetActiveDoctorsAsync();
            ViewBag.Patients = await GetActivePatientsAsync();
        }

        private async Task LoadCreateScheduleViewDataAsync()
        {
            ViewBag.Doctors = await GetActiveDoctorsAsync();
        }

        private async Task<List<Doctor>> GetActiveDoctorsAsync()
        {
            return await _context.Doctors.Where(d => d.IsActive).ToListAsync();
        }

        private async Task<List<ApplicationUser>> GetActivePatientsAsync()
        {
            return await _context.Users
                .Where(u => u.Role == "Client" && u.IsApproved && u.IsActive)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();
        }

        private async Task<List<Schedule>> GetActiveSchedulesAsync()
        {
            return await _context.Schedules
                .Include(s => s.Doctor)
                .Where(s => s.IsActive) // Only show active schedules
                .OrderBy(s => s.Doctor.Name)
                .ThenBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }

        private async Task<Schedule?> GetScheduleByIdAsync(int id)
        {
            return await _context.Schedules
                .Include(s => s.Doctor)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        private async Task<bool> ScheduleExistsAsync(int id)
        {
            return await _context.Schedules.AnyAsync(e => e.Id == id);
        }
    }
}