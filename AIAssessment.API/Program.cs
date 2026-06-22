using AIAssessment.API.Hubs;
using AIAssessment.API.Middleware;
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
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
                
});

var app = builder.Build();

await DependencyInjection.SeedDatabaseAsync(app.Services);

// ── Middleware pipeline (ORDER MATTERS) 

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
//    Must be AFTER UseAuthentication() (needs the token read first)
//    and BEFORE UseAuthorization() (revoked tokens must be blocked before role checks)
app.UseMiddleware<TokenValidationMiddleware>();

// 4. Enforces [Authorize] and [Authorize(Roles="...")]
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