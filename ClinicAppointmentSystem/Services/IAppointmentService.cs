using ClinicAppointmentSystem.Models;

namespace ClinicAppointmentSystem.Services
{
    public interface IAppointmentService
    {
        Task<string> GenerateAppointmentIdAsync();
        Task<bool> CanPatientBookAppointmentAsync(string patientId, DateTime date);
        Task<int> GetBookedSlotsCountAsync(int doctorId, DateTime date, TimeSpan startTime, TimeSpan endTime);
        Task<List<TimeSpan>> GetAvailableTimeSlotsAsync(int doctorId, DateTime date);
    }
}