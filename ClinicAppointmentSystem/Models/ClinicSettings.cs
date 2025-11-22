using System.ComponentModel.DataAnnotations;

namespace ClinicAppointmentSystem.Models
{
    public class ClinicSettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Clinic Name")]
        public string ClinicName { get; set; } = "Smart Clinic";

        [Required]
        [Display(Name = "Opening Time")]
        public TimeSpan OpeningTime { get; set; } = new TimeSpan(9, 0, 0);

        [Required]
        [Display(Name = "Closing Time")]
        public TimeSpan ClosingTime { get; set; } = new TimeSpan(17, 0, 0);

        [Display(Name = "Appointment Duration (minutes)")]
        public int AppointmentDuration { get; set; } = 30;

        [Display(Name = "Max Appointments Per Day Per Patient")]
        public int MaxAppointmentsPerDay { get; set; } = 1;

        public string Holidays { get; set; } = "[]"; // JSON array of holiday dates

        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }
}