using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ClinicAppointmentSystem.Data;
using ClinicAppointmentSystem.Models;

[assembly: HostingStartup(typeof(ClinicAppointmentSystem.Areas.Identity.IdentityHostingStartup))]

namespace ClinicAppointmentSystem.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
                // This is already configured in Program.cs, but can be used for additional Identity configuration
            });
        }
    }
}