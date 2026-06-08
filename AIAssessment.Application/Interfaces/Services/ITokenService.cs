using System;
using System.Threading.Tasks;

namespace AIAssessment.Application.Interfaces.Services
{
    public interface ITokenService
    {
      
        /// Generates a JWT for the given userId, email and role.
        /// Takes primitives instead of a domain User so the actual
        /// Identity user ID (not always 0) is embedded in the token.
      
        Task<(string Token, string JwtId, DateTime ExpiresAt)> GenerateTokenAsync(
            int userId, string email, string role);

        //Reads the jti claim from a raw token string.
        string? GetJwtIdFromToken(string token);
    }
}