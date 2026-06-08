using System; 
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AIAssessment.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AIAssessment.Infrastructure.Services
{
    public class JwtTokenService : ITokenService
    {
        private readonly IConfiguration _config;

        public JwtTokenService(IConfiguration config) => _config = config;

        public Task<(string Token, string JwtId, DateTime ExpiresAt)> GenerateTokenAsync(
            int userId, string email, string role)
        {
            var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured.");
            var issuer = _config["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer not configured.");
            var audience = _config["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience not configured.");
            int expiry = int.TryParse(_config["Jwt:ExpiryMinutes"], out var m) ? m : 60;

            var jwtId = Guid.NewGuid().ToString();
            var expiresAt = DateTime.UtcNow.AddMinutes(expiry);

            var claims = new List<Claim>
            {
                // userId is the real Identity user ID from AspNetUsers.Id
                // This is what GetCurrentUserId() reads in the controllers
                new(JwtRegisteredClaimNames.Sub,   userId.ToString()),
                new(JwtRegisteredClaimNames.Email, email),
                new(JwtRegisteredClaimNames.Jti,   jwtId),
                new(ClaimTypes.Role,               role),
                new(ClaimTypes.NameIdentifier,     userId.ToString())
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            return Task.FromResult((new JwtSecurityTokenHandler().WriteToken(token), jwtId, expiresAt));
        }

        public string? GetJwtIdFromToken(string token)
        {
            try { return new JwtSecurityTokenHandler().ReadJwtToken(token).Id; }
            catch { return null; }
        }


    }
}