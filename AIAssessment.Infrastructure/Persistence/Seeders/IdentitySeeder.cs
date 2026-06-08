using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AIAssessment.Infrastructure.Persistence.Seeders;


// Runs once on application startup to ensure the database has:
//   1. The "Admin" and "Candidate" roles
//   2. A default admin user

// This is idempotent — safe to run every startup.
// It checks before creating, so it never duplicates data.

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser<int>>>();
        var logger = services.GetRequiredService<ILogger<AppDbContext>>();

        // --- 1. Seed roles ---
        string[] roles = { "Admin", "Candidate" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<int>(role));
                if (result.Succeeded)
                    logger.LogInformation("Role '{Role}' created.", role);
                else
                    logger.LogError("Failed to create role '{Role}': {Errors}",
                        role, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        // --- 2. Seed admin user ---
        const string adminEmail = "admin@aiassessment.com";
        const string adminPassword = "Admin@123";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new IdentityUser<int>
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                logger.LogError("Failed to create admin user: {Errors}", errors);
                throw new InvalidOperationException($"Admin seed failed: {errors}");
            }

            logger.LogInformation("Admin user created: {Email}", adminEmail);
        }

        // --- 3. Assign Admin role ---
        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
            logger.LogInformation("Admin role assigned to: {Email}", adminEmail);
        }
    }
}