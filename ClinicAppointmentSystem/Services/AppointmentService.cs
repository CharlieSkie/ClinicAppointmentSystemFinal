using ClinicAppointmentSystem.Data;
using ClinicAppointmentSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointmentSystem.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly ApplicationDbContext _context;

        public AppointmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateAppointmentIdAsync()
        {
            var lastAppointment = await _context.Appointments
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastAppointment != null && lastAppointment.AppointmentId.StartsWith("SC-"))
            {
                if (int.TryParse(lastAppointment.AppointmentId.Substring(3), out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"SC-{nextNumber:D3}";
        }

        public async Task<bool> CanPatientBookAppointmentAsync(string patientId, DateTime date)
        {
            var existingAppointment = await _context.Appointments
                .Where(a => a.PatientId == patientId
                         && a.AppointmentDate.Date == date.Date
                         && (a.Status == "Pending" || a.Status == "Confirmed"))
                .FirstOrDefaultAsync();

            return existingAppointment == null;
        }

        public async Task<int> GetBookedSlotsCountAsync(int doctorId, DateTime date, TimeSpan startTime, TimeSpan endTime)
        {
            return await _context.Appointments
                .Where(a => a.DoctorId == doctorId
                         && a.AppointmentDate.Date == date.Date
                         && a.StartTime == startTime
                         && a.EndTime == endTime
                         && (a.Status == "Pending" || a.Status == "Confirmed"))
                .CountAsync();
        }

        public async Task<List<TimeSpan>> GetAvailableTimeSlotsAsync(int doctorId, DateTime date)
        {
            var schedule = await _context.Schedules
                .FirstOrDefaultAsync(s => s.DoctorId == doctorId && s.DayOfWeek == date.DayOfWeek && s.IsActive);

            if (schedule == null)
                return new List<TimeSpan>();

            var timeSlots = new List<TimeSpan>();
            var currentTime = schedule.StartTime;
            var clinicSettings = await _context.ClinicSettings.FirstOrDefaultAsync() ?? new ClinicSettings();

            while (currentTime < schedule.EndTime)
            {
                var endTime = currentTime.Add(TimeSpan.FromMinutes(clinicSettings.AppointmentDuration));

                if (endTime <= schedule.EndTime)
                {
                    timeSlots.Add(currentTime);
                }

                currentTime = endTime;
            }

            return timeSlots;
        }
    }
}