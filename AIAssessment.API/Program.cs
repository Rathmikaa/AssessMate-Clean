// ── AIAssessment.API/Program.cs ───────────────────────────────────────────────
// REPLACE your existing Program.cs with this file.
//
// What changed vs the original:
//   • Added builder.Services.AddAuthorization(...) with named policies
//     RequireSuperAdmin and RequireEvaluator.
//   • Everything else is identical.
// ─────────────────────────────────────────────────────────────────────────────

using AIAssessment.API.Hubs;
using AIAssessment.API.Middleware;
using AIAssessment.Application.Common;
using AIAssessment.Application.Interfaces.Services;
using AIAssessment.Infrastructure;

using DotNetEnv;
using Microsoft.OpenApi.Models;


var envFile = FindEnvFile(AppContext.BaseDirectory);
if (envFile != null)
{
    Env.Load(envFile);
}

var builder = WebApplication.CreateBuilder(args);


var envOverrides = new Dictionary<string, string?>
{
    ["Email:Host"] = Environment.GetEnvironmentVariable("Email__Host"),
    ["Email:Port"] = Environment.GetEnvironmentVariable("Email__Port"),
    ["Email:EnableSsl"] = Environment.GetEnvironmentVariable("Email__EnableSsl"),
    ["Email:Username"] = Environment.GetEnvironmentVariable("Email__Username"),
    ["Email:Password"] = Environment.GetEnvironmentVariable("Email__Password"),
    ["Jwt:Key"] = Environment.GetEnvironmentVariable("Jwt__Key"),
};

builder.Configuration.AddInMemoryCollection(
    envOverrides.Where(kvp => !string.IsNullOrEmpty(kvp.Value)).ToList());


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSignalR();
builder.Services.AddScoped<IAssessmentMonitorNotifier, SignalRAssessmentMonitorNotifier>();


//  NEW: named authorization policies
// RequireSuperAdmin  → only SuperAdmin tokens pass
// RequireEvaluator   → both Evaluator AND SuperAdmin tokens pass
//                      (SuperAdmin inherits all Evaluator permissions)
//
// Usage in controllers:
//   [Authorize(Policy = "RequireSuperAdmin")]
//   [Authorize(Policy = "RequireEvaluator")]
//
// The SuperAdminController uses [Authorize(Roles = Roles.SuperAdmin)] directly,
// but these policies are available for any fine-grained endpoint-level control.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireSuperAdmin", policy =>
        policy.RequireRole(Roles.SuperAdmin));

    options.AddPolicy("RequireEvaluator", policy =>
        policy.RequireRole(Roles.Evaluator, Roles.SuperAdmin));
});



builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AI Assessment API",
        Version = "v1",
        Description = "Assessment platform for recruitment — Clean Architecture"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste your JWT token here."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(
                  "http://localhost:3000",
                  "http://localhost:5173",
                  "http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});


var app = builder.Build();

await DependencyInjection.SeedDatabaseAsync(app.Services);

// Middleware pipeline

// 1. Global error handler — must be outermost
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AI Assessment API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

// 2. Validates JWT signature + populates User.Claims
app.UseAuthentication();

// 3. Checks the token still exists in UserTokens table.
//    Must be AFTER UseAuthentication() and BEFORE UseAuthorization()
app.UseMiddleware<TokenValidationMiddleware>();

// 4. Enforces [Authorize] and [Authorize(Roles="...")] / [Authorize(Policy="...")]
app.UseAuthorization();

app.MapControllers();
app.MapHub<AssessmentMonitorHub>("/hubs/assessment-monitor");
app.Run();


static string? FindEnvFile(string start, string fileName = ".env")
{
    var dir = new DirectoryInfo(start);
    while (dir != null)
    {
        var candidate = Path.Combine(dir.FullName, fileName);
        if (File.Exists(candidate))
            return candidate;
        dir = dir.Parent;
    }
    return null;
}