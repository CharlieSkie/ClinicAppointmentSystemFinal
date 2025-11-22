using System.ComponentModel.DataAnnotations;

namespace ClinicAppointmentSystem.Models
{
    public class Schedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        [Display(Name = "Day of Week")]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        [Display(Name = "Start Time")]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Display(Name = "End Time")]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "Max Appointments")]
        public int MaxAppointments { get; set; } = 10;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual Doctor Doctor { get; set; } = null!;
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}