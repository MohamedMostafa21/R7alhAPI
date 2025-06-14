using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using R7alaAPI.Models;

namespace R7alaAPI.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var context = new ApplicationDBContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDBContext>>());

            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            string[] roleNames = { "Admin", "User", "TourGuide" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(new ApplicationRole(roleName));
                    if (!result.Succeeded)
                    {
                        throw new Exception($"Failed to create role {roleName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }

            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            var adminEmail = "admin@example.com";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Mohamed",
                    LastName = "Mostafa",
                    Gender = Gender.Male, // Adjust if Gender is not an enum
                    DateOfBirth = new DateTime(2000, 1, 1),
                    City = "Assiut",
                    Country = "Egypt",
                    ProfilePictureUrl = "G:\\Gallery\\pp.jpg",
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@1234");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
                else
                {
                    throw new Exception($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}