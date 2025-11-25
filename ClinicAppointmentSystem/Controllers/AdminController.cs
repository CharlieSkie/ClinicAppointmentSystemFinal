using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicAppointmentSystem.Models;
using ClinicAppointmentSystem.Data;

namespace ClinicAppointmentSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var dashboardStats = new
            {
                TotalUsers = await _context.Users.CountAsync(u => !u.IsDeleted),
                PendingApprovals = await _context.Users.CountAsync(u => !u.IsApproved && !u.IsDeleted),
                TotalAppointments = await _context.Appointments.CountAsync(),
                TodayAppointments = await _context.Appointments.CountAsync(a => a.AppointmentDate.Date == DateTime.Today),
                TotalDoctors = await _context.Doctors.CountAsync(),
                ActiveDoctors = await _context.Doctors.CountAsync(d => d.IsActive)
            };

            ViewBag.DashboardStats = dashboardStats;
            return View();
        }

        public async Task<IActionResult> UserManagement()
        {
            var users = await _context.Users
                .Where(u => !u.IsDeleted) // Exclude soft-deleted users
                .OrderByDescending(u => u.CreatedDate)
                .ToListAsync();
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.IsDeleted)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(ApplicationUser model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user != null && !user.IsDeleted)
                {
                    user.FirstName = model.FirstName;
                    user.LastName = model.LastName;
                    user.PhoneNumber = model.PhoneNumber;
                    user.Email = model.Email;
                    user.UserName = model.Email;

                    var result = await _userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        TempData["SuccessMessage"] = "User updated successfully.";
                        return RedirectToAction(nameof(UserManagement));
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "User not found.";
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && !user.IsDeleted)
            {
                user.IsApproved = true;
                user.IsActive = true;
                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = "User approved successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "User not found.";
            }
            return RedirectToAction(nameof(UserManagement));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && !user.IsDeleted)
            {
                // Soft delete instead of hard delete for rejection
                user.IsDeleted = true;
                user.IsActive = false;
                user.DeletedDate = DateTime.UtcNow;
                user.Email = $"{user.Email}.rejected.{DateTime.UtcNow.Ticks}";
                user.UserName = $"{user.UserName}.rejected.{DateTime.UtcNow.Ticks}";
                user.NormalizedEmail = user.Email.ToUpper();
                user.NormalizedUserName = user.UserName.ToUpper();

                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = "User rejected successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "User not found.";
            }
            return RedirectToAction(nameof(UserManagement));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && !user.IsDeleted)
            {
                user.IsActive = !user.IsActive;
                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = $"User {(user.IsActive ? "activated" : "deactivated")} successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "User not found.";
            }
            return RedirectToAction(nameof(UserManagement));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == userId)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(UserManagement));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && !user.IsDeleted)
            {
                try
                {
                    // SOFT DELETE: Mark as deleted instead of physically deleting
                    user.IsDeleted = true;
                    user.IsActive = false;
                    user.DeletedDate = DateTime.UtcNow;

                    // Update email and username to avoid conflicts if same user is recreated
                    user.Email = $"{user.Email}.deleted.{DateTime.UtcNow.Ticks}";
                    user.UserName = $"{user.UserName}.deleted.{DateTime.UtcNow.Ticks}";
                    user.NormalizedEmail = user.Email.ToUpper();
                    user.NormalizedUserName = user.UserName.ToUpper();

                    var result = await _userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        TempData["SuccessMessage"] = "User deleted successfully.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Error deleting user.";
                        foreach (var error in result.Errors)
                        {
                            TempData["ErrorMessage"] += $" {error.Description}";
                        }
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error deleting user: {ex.Message}";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "User not found.";
            }

            return RedirectToAction(nameof(UserManagement));
        }

        // Doctor Management
        public async Task<IActionResult> Doctors()
        {
            var doctors = await _context.Doctors.ToListAsync();
            return View(doctors);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDoctor(Doctor doctor)
        {
            if (ModelState.IsValid)
            {
                doctor.CreatedDate = DateTime.UtcNow;
                _context.Doctors.Add(doctor);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Doctor added successfully.";
                return RedirectToAction(nameof(Doctors));
            }

            TempData["ErrorMessage"] = "Please correct the errors below.";
            return View("Doctors", await _context.Doctors.ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleDoctorStatus(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor != null)
            {
                doctor.IsActive = !doctor.IsActive;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Doctor {(doctor.IsActive ? "activated" : "deactivated")} successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Doctor not found.";
            }
            return RedirectToAction(nameof(Doctors));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor != null)
            {
                // Check if doctor has appointments
                var hasAppointments = await _context.Appointments.AnyAsync(a => a.DoctorId == id);
                if (hasAppointments)
                {
                    TempData["ErrorMessage"] = "Cannot delete doctor with existing appointments.";
                    return RedirectToAction(nameof(Doctors));
                }

                _context.Doctors.Remove(doctor);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Doctor deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Doctor not found.";
            }
            return RedirectToAction(nameof(Doctors));
        }

        // Appointment Management
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

        public async Task<IActionResult> DoctorSchedules()
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
    }
}