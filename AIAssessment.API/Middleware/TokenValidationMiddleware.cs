using AIAssessment.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace AIAssessment.API.Middleware
{
    
    /// Runs AFTER UseAuthentication() on every request that carries a Bearer token.
    ///
    /// WHY THIS EXISTS:
    ///   UseAuthentication() checks JWT signature and expiry — but it has no idea
    ///   whether the token was deleted from the DB (logged out).
    ///   This middleware fills that gap:
    ///     - Extract jti from the Bearer token
    ///     - Look it up in UserTokens table
    ///     - If missing → 401 (token was revoked / user logged out)
    ///     - If found   → continue
    ///
   
    
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
         
        public TokenValidationMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context, IUserTokenRepository tokenRepo)
        {
            var authHeader = context.Request.Headers.Authorization.ToString();

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var rawToken = authHeader["Bearer ".Length..].Trim();
                var jwtId = GetJwtId(rawToken);

                if (jwtId != null)
                {
                    var stored = await tokenRepo.GetByJwtIdAsync(jwtId);

                    if (stored == null)
                    {
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(
                            """{"status":401,"error":"Token has been revoked. Please log in again."}""");
                        return;
                    }

                    if (stored.ExpiresAt < DateTime.UtcNow)
                    {
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(
                            """{"status":401,"error":"Token has expired. Please log in again."}""");
                        return;
                    }
                }
            }

            await _next(context);
        }

        private static string? GetJwtId(string token)
        {
            try { return new JwtSecurityTokenHandler().ReadJwtToken(token).Id; }
            catch { return null; }
        }
    }
}