using System.Net;
using System.Text.Json;
using AIAssessment.Domain.Exceptions;

namespace AIAssessment.API.Middleware;


// Catches all unhandled exceptions and returns a consistent JSON error shape.
//   DomainException  → 400 Bad Request  (business rule violation, safe to show)
//   Any other        → 500 Internal     (unexpected; generic message in production)

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next,
        ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Domain rule violation: {Message}", ex.Message);
            await WriteError(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Method} {Path}",
                context.Request.Method, context.Request.Path);

            var message = _env.IsDevelopment()
                ? ex.Message
                : "An unexpected error occurred. Please try again later.";

            await WriteError(context, HttpStatusCode.InternalServerError, message);
        }
    }

    private static async Task WriteError(HttpContext ctx, HttpStatusCode status, string message)
    {
        ctx.Response.StatusCode = (int)status;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = (int)status,
            error = message
        }));
    }
}