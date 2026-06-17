using AIAssessment.Domain.Entities;
using System.Threading.Tasks;

namespace AIAssessment.Application.Interfaces.Repositories
{
    public interface IRefreshTokenRepository
    {
        //Save a newly is sued refresh token.
        Task<RefreshToken> AddAsync(RefreshToken token);
      
        /// Find a refresh token by its value.
        /// Used in /api/auth/refresh to validate the incoming token.
        
        Task<RefreshToken?> GetByTokenAsync(string token);

        //Delete a specific refresh token. Called on refresh (rotation).
        Task<bool> RevokeAsync(string token);

       
        /// Delete ALL refresh tokens for a user.
        /// Called on login (one session per user) and logout.
       
        Task RevokeAllForUserAsync(int userId);

        //Housekeeping — remove expired tokens.
        Task DeleteExpiredAsync();
    }
}