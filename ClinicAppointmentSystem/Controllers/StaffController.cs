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

        public StaffController(
            ApplicationDbContext context,
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
                    .CountAsync(u => u.Role == "Client" && u.IsApproved && u.IsActive && !u.IsDeleted),
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
            await LoadCreateAppointmentViewData();
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
                if (string.IsNullOrEmpty(patientId) || doctorId == 0)
                {
                    TempData["ErrorMessage"] = "Please fill all required fields.";
                    await LoadCreateAppointmentViewData();
                    return View("CreateAppointment");
                }

                var appointmentId = await _appointmentService.GenerateAppointmentIdAsync();
                var appointment = new Appointment
                {
                    PatientId = patientId,
                    DoctorId = doctorId,
                    ServiceId = 1,
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
                await LoadCreateAppointmentViewData();
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
            var patients = await _context.Users
                .Where(u => u.Role == "Client" && u.IsApproved && u.IsActive && !u.IsDeleted)
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
            await LoadCreateScheduleViewData();
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
                if (doctorId == 0)
                {
                    TempData["ErrorMessage"] = "Please select a doctor.";
                    await LoadCreateScheduleViewData();
                    return View("CreateSchedule");
                }

                var schedule = new Schedule
                {
                    DoctorId = doctorId,
                    DayOfWeek = (DayOfWeek)dayOfWeek,
                    StartTime = startTime,
                    EndTime = endTime,
                    MaxAppointments = maxAppointments,
                    IsActive = true
                };

                _context.Schedules.Add(schedule);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Schedule created successfully.";
                return RedirectToAction(nameof(Schedule));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating schedule: {ex.Message}";
                await LoadCreateScheduleViewData();
                return View("CreateSchedule");
            }
        }

        public async Task<IActionResult> EditSchedule(int id)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Doctor)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (schedule == null)
            {
                TempData["ErrorMessage"] = "Schedule not found.";
                return RedirectToAction(nameof(Schedule));
            }

            await LoadCreateScheduleViewData();
            return View(schedule);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSchedule(Schedule model)
        {
            if (model == null)
            {
                TempData["ErrorMessage"] = "Invalid schedule data.";
                return RedirectToAction(nameof(Schedule));
            }

            try
            {
                // Find the existing schedule
                var existingSchedule = await _context.Schedules
                    .FirstOrDefaultAsync(s => s.Id == model.Id);

                if (existingSchedule == null)
                {
                    TempData["ErrorMessage"] = "Schedule not found.";
                    return RedirectToAction(nameof(Schedule));
                }

                // Update properties
                existingSchedule.DoctorId = model.DoctorId;
                existingSchedule.DayOfWeek = model.DayOfWeek;
                existingSchedule.StartTime = model.StartTime;
                existingSchedule.EndTime = model.EndTime;
                existingSchedule.MaxAppointments = model.MaxAppointments;
                existingSchedule.IsActive = model.IsActive;

                // Save changes
                _context.Schedules.Update(existingSchedule);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Schedule updated successfully!";
                return RedirectToAction(nameof(Schedule));
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorMessage"] = $"Database error: {ex.Message}";
                await LoadCreateScheduleViewData();
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating schedule: {ex.Message}";
                await LoadCreateScheduleViewData();
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            try
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
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting schedule: {ex.Message}";
            }

            return RedirectToAction(nameof(Schedule));
        }

        // Helper methods
        private async Task LoadCreateAppointmentViewData()
        {
            ViewBag.Doctors = await _context.Doctors.Where(d => d.IsActive).ToListAsync();
            ViewBag.Patients = await _context.Users
                .Where(u => u.Role == "Client" && u.IsApproved && u.IsActive && !u.IsDeleted)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();
        }

        private async Task LoadCreateScheduleViewData()
        {
            ViewBag.Doctors = await _context.Doctors.Where(d => d.IsActive).ToListAsync();
        }
    }
}