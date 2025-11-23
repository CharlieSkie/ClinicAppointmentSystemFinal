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

                // ✅ REMOVED: Default sample doctors - Admin will add doctors manually

                // Create sample services (keeping for reference, but not used in appointments)
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
                    Console.WriteLine("✅ Sample services created (for reference).");
                }

                // ✅ REMOVED: Default schedules - Staff will create schedules manually

                Console.WriteLine("🎉 Database initialization completed successfully!");
            }
        }
    }
}