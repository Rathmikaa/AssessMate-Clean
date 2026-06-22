using AIAssessment.Application.Interfaces.Repositories;
using AIAssessment.Application.Interfaces.Services;
using AIAssessment.Application.Services;
using AIAssessment.Infrastructure.Identity;
using AIAssessment.Infrastructure.Persistence;
using AIAssessment.Infrastructure.Persistence.Repositories;
using AIAssessment.Infrastructure.Persistence.Seeders;
using AIAssessment.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace AIAssessment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. EF Core 
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly("AIAssessment.Infrastructure")));

        // Must be registered BEFORE AddIdentityCore — AddIdentityCore only TryAdds the
        // default IUserValidator, so whichever implementation lands first wins.
        services.AddScoped<IUserValidator<IdentityUser<int>>, EmailOnlyUserValidator>();
        
        services
            .AddIdentityCore<IdentityUser<int>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;

                // Brute-force protection — paired with AuthService's lockoutOnFailure: true (Phase 2)
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole<int>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        // 3. JWT Authentication
        var jwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                                                  Encoding.UTF8.GetBytes(jwtKey)),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.NameIdentifier
                };
            });

        services.AddAuthorization();

        // 4. Repositories
        services.AddScoped<IAssessmentRepository, AssessmentRepository>();
        services.AddScoped<IQuestionRepository, QuestionRepository>();
        services.AddScoped<ISubmissionRepository, SubmissionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserTokenRepository, UserTokenRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>(); // 

        // 5. Services
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IScoringService, KeywordScoringService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<AuthService>();
        services.AddScoped<AssessmentService>();
        services.AddScoped<SubmissionService>();
        services.AddScoped<QuestionService>();
        services.AddScoped<CandidateService>();

        return services;
    }

    public static async Task SeedDatabaseAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        await IdentitySeeder.SeedAsync(scope.ServiceProvider);
    }
}