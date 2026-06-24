using AIAssessment.Application.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AIAssessment.Infrastructure.Persistence.Seeders;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser<int>>>();
        var logger = services.GetRequiredService<ILogger<object>>();
        var config = services.GetRequiredService<IConfiguration>();

        await SeedRolesAsync(roleManager, logger);
        await SeedSuperAdminAsync(userManager, logger, config);

        // ── Optional: run ONCE to migrate old "Admin" users → "Evaluator" ──────
        // After running, comment this line out (or delete it).
        await MigrateAdminToEvaluatorAsync(userManager, roleManager, logger);
    }

    // ── 1. Ensure all three roles exist ──────────────────────────────────────
    private static async Task SeedRolesAsync(
        RoleManager<IdentityRole<int>> roleManager,
        ILogger logger)
    {
        string[] roles = [Roles.SuperAdmin, Roles.Evaluator, Roles.Candidate];

        foreach (var roleName in roles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            var result = await roleManager.CreateAsync(new IdentityRole<int>(roleName));

            if (result.Succeeded)
                logger.LogInformation("Role '{Role}' created.", roleName);
            else
                logger.LogError("Failed to create role '{Role}': {Errors}",
                    roleName,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    // ── 2. Seed one SuperAdmin user from appsettings / env vars ──────────────
    //
    // Add this block to appsettings.json (override via env vars in production):
    //
    //   "SuperAdmin": {
    //     "Email":    "superadmin@assessmate.com",
    //     "Password": "SuperAdmin@123",
    //     "FullName": "Super Admin"
    //   }
    //
    private static async Task SeedSuperAdminAsync(
        UserManager<IdentityUser<int>> userManager,
        ILogger logger,
        IConfiguration config)
    {
        var email = config["SuperAdmin:Email"] ?? "superadmin@assessmate.com";
        var password = config["SuperAdmin:Password"] ?? "SuperAdmin@123";
        var fullName = config["SuperAdmin:FullName"] ?? "Super Admin";

        // Already exists — nothing to do
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var superAdmin = new IdentityUser<int>
        {
            UserName = fullName,   // matches your convention: UserName = display name
            Email = email,
            EmailConfirmed = true        // skip email-confirmation flow for seeded account
        };

        var result = await userManager.CreateAsync(superAdmin, password);

        if (!result.Succeeded)
        {
            logger.LogError("Failed to seed SuperAdmin '{Email}': {Errors}",
                email,
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(superAdmin, Roles.SuperAdmin);
        logger.LogInformation("SuperAdmin '{Email}' seeded successfully.", email);
    }

    // ── 3. One-time migration: move existing "Admin" users → "Evaluator" ─────
    // Uncomment the call in SeedAsync() above, run the app once, then re-comment.
    private static async Task MigrateAdminToEvaluatorAsync(
        UserManager<IdentityUser<int>> userManager,
        RoleManager<IdentityRole<int>> roleManager,
        ILogger logger)
    {
        var oldRoleName = "Admin";
        var oldRole = await roleManager.FindByNameAsync(oldRoleName);

        if (oldRole is null)
        {
            logger.LogInformation("No '{OldRole}' role found — migration skipped.", oldRoleName);
            return;
        }

        var admins = await userManager.GetUsersInRoleAsync(oldRoleName);
        logger.LogInformation("Migrating {Count} user(s) from '{OldRole}' → '{NewRole}'.",
            admins.Count, oldRoleName, Roles.Evaluator);

        foreach (var admin in admins)
        {
            await userManager.AddToRoleAsync(admin, Roles.Evaluator);
            await userManager.RemoveFromRoleAsync(admin, oldRoleName);
            logger.LogInformation("  Migrated user '{Email}'.", admin.Email);
        }

        await roleManager.DeleteAsync(oldRole);
        logger.LogInformation("Deleted old '{OldRole}' role.", oldRoleName);
    }
}