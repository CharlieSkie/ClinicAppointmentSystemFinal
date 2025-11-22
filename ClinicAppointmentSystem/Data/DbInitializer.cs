using Microsoft.AspNetCore.Identity;
using ClinicAppointmentSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointmentSystem.Data
{
    public class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider, string adminPassword)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // Ensure database is created
                await context.Database.MigrateAsync();

                var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                // Create roles
                string[] roleNames = { "Admin", "Staff", "Client" };
                foreach (var roleName in roleNames)
                {
                    var roleExist = await roleManager.RoleExistsAsync(roleName);
                    if (!roleExist)
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // ✅ FIX: Create default admin with matching username and email
                var adminUser = await userManager.FindByEmailAsync("admin@clinic.com");
                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = "admin@clinic.com", // ✅ FIX: Use email as username
                        Email = "admin@clinic.com",
                        FirstName = "System",
                        LastName = "Admin",
                        Role = "Admin",
                        IsApproved = true,
                        IsActive = true,
                        EmailConfirmed = true
                    };

                    var createPowerUser = await userManager.CreateAsync(adminUser, adminPassword);
                    if (createPowerUser.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                        Console.WriteLine("✅ Admin user created successfully!");
                    }
                    else
                    {
                        Console.WriteLine("❌ ERROR creating admin user:");
                        foreach (var error in createPowerUser.Errors)
                        {
                            Console.WriteLine($" - {error.Description}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("ℹ️ Admin user already exists.");
                }

                // Create sample doctors
                if (!context.Doctors.Any())
                {
                    context.Doctors.AddRange(
                        new Doctor
                        {
                            Name = "Dr. Sarah Johnson",
                            Specialization = "Cardiology",
                            Email = "sarah.johnson@clinic.com",
                            Phone = "555-0101",
                            IsActive = true,
                            CreatedDate = DateTime.UtcNow
                        },
                        new Doctor
                        {
                            Name = "Dr. Michael Chen",
                            Specialization = "Dermatology",
                            Email = "michael.chen@clinic.com",
                            Phone = "555-0102",
                            IsActive = true,
                            CreatedDate = DateTime.UtcNow
                        },
                        new Doctor
                        {
                            Name = "Dr. Emily Davis",
                            Specialization = "Pediatrics",
                            Email = "emily.davis@clinic.com",
                            Phone = "555-0103",
                            IsActive = true,
                            CreatedDate = DateTime.UtcNow
                        }
                    );
                    await context.SaveChangesAsync();
                    Console.WriteLine("✅ Sample doctors created.");
                }

                // Create sample services
                if (!context.Services.Any())
                {
                    context.Services.AddRange(
                        new Service
                        {
                            Name = "General Consultation",
                            Description = "Routine health checkup and consultation",
                            Price = 100.00m,
                            DurationMinutes = 30,
                            IsActive = true,
                            CreatedDate = DateTime.UtcNow
                        },
                        new Service
                        {
                            Name = "Specialist Consultation",
                            Description = "Specialized medical consultation",
                            Price = 200.00m,
                            DurationMinutes = 45,
                            IsActive = true,
                            CreatedDate = DateTime.UtcNow
                        },
                        new Service
                        {
                            Name = "Follow-up Visit",
                            Description = "Post-treatment follow-up appointment",
                            Price = 75.00m,
                            DurationMinutes = 20,
                            IsActive = true,
                            CreatedDate = DateTime.UtcNow
                        }
                    );
                    await context.SaveChangesAsync();
                    Console.WriteLine("✅ Sample services created.");
                }

                // Create schedules for doctors
                if (!context.Schedules.Any())
                {
                    var doctors = await context.Doctors.ToListAsync();
                    foreach (var doctor in doctors)
                    {
                        for (int day = 1; day <= 5; day++) // Monday to Friday
                        {
                            context.Schedules.Add(new Schedule
                            {
                                DoctorId = doctor.Id,
                                DayOfWeek = (DayOfWeek)day,
                                StartTime = new TimeSpan(9, 0, 0),
                                EndTime = new TimeSpan(17, 0, 0),
                                MaxAppointments = 16,
                                IsActive = true
                            });
                        }
                    }
                    await context.SaveChangesAsync();
                    Console.WriteLine("✅ Doctor schedules created.");
                }

                Console.WriteLine("🎉 Database initialization completed successfully!");
            }
        }
    }
}