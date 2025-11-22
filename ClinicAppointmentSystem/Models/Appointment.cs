using System.ComponentModel.DataAnnotations;

namespace ClinicAppointmentSystem.Models
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Appointment ID")]
        public string AppointmentId { get; set; } = string.Empty; // SC-001 format

        [Required]
        public string PatientId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Doctor")]
        public int DoctorId { get; set; }

        [Required]
        [Display(Name = "Service")]
        public int ServiceId { get; set; }

        [Required]
        [Display(Name = "Appointment Date")]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }

        [Required]
        [Display(Name = "Start Time")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Display(Name = "End Time")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Completed, Cancelled

        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ApplicationUser Patient { get; set; } = null!;
        public virtual Doctor Doctor { get; set; } = null!;
        public virtual Service Service { get; set; } = null!;
    }
}