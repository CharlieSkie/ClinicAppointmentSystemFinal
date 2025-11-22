using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ClinicAppointmentSystem.Models;

namespace ClinicAppointmentSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<ClinicSettings> ClinicSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure relationships
            builder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(u => u.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Appointment>()
                .HasOne(a => a.Service)
                .WithMany(s => s.Appointments)
                .HasForeignKey(a => a.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Schedule>()
                .HasOne(s => s.Doctor)
                .WithMany(d => d.Schedules)
                .HasForeignKey(s => s.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraints
            builder.Entity<Appointment>()
                .HasIndex(a => a.AppointmentId)
                .IsUnique();

            // Prevent duplicate appointments for same patient on same day
            builder.Entity<Appointment>()
                .HasIndex(a => new { a.PatientId, a.AppointmentDate })
                .HasFilter("[Status] IN ('Pending', 'Confirmed')");

            // Seed data
            builder.Entity<ClinicSettings>().HasData(
                new ClinicSettings
                {
                    Id = 1,
                    ClinicName = "Smart Clinic",
                    OpeningTime = new TimeSpan(9, 0, 0),
                    ClosingTime = new TimeSpan(17, 0, 0),
                    AppointmentDuration = 30,
                    MaxAppointmentsPerDay = 1,
                    Holidays = "[]",
                    UpdatedDate = DateTime.UtcNow
                }
            );
        }
    }
}