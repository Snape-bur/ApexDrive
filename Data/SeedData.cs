using System;
using System.Linq;
using System.Threading.Tasks;
using ApexDrive.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace ApexDrive.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // ✅ Ensure database exists
            await context.Database.MigrateAsync();

            // 🧭 Step 1 — Seed Roles
            string[] roleNames = { "SuperAdmin", "Admin", "Customer" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            // 🧭 Step 2 — Seed SuperAdmin
            string superAdminEmail = "superadmin@apexdrive.com";
            string superAdminPassword = "Admin@123";

            var superAdminUser = await userManager.FindByEmailAsync(superAdminEmail);
            if (superAdminUser == null)
            {
                superAdminUser = new AppUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(superAdminUser, superAdminPassword);
                if (createResult.Succeeded)
                    await userManager.AddToRoleAsync(superAdminUser, "SuperAdmin");
            }

            // 🧭 Step 3 — Seed Branches
            if (!context.Branches.Any())
            {
                var branches = new[]
                {
                    new Branch
                    {
                        BranchName = "Bangkok Branch",
                        Address = "123 Sukhumvit Rd, Bangkok",
                        Phone = "02-123-4567",
                        Email = "bangkok@apexdrive.com"
                    },
                    new Branch
                    {
                        BranchName = "Chiang Mai Branch",
                        Address = "45 Nimmanhaemin Rd, Chiang Mai",
                        Phone = "053-234-567",
                        Email = "chiangmai@apexdrive.com"
                    },
                    new Branch
                    {
                        BranchName = "Phuket Branch",
                        Address = "89 Patong Beach Rd, Phuket",
                        Phone = "076-345-678",
                        Email = "phuket@apexdrive.com"
                    }
                };

                context.Branches.AddRange(branches);
                await context.SaveChangesAsync();
            }
        }
    }
}
