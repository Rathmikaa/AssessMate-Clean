  using AIAssessment.API.Middleware;
using AIAssessment.Infrastructure;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddInfrastructure(builder.Configuration);



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
              .AllowAnyHeader());
                
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
app.Run(); 