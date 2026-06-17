using AIAssessment.Domain.Entities;
using System.Threading.Tasks;

namespace AIAssessment.Application.Interfaces.Repositories
{
    public interface IUserTokenRepository
    { 
        /// Save a newly issued token.
        Task<UserToken> AddAsync(UserToken token);

        
        /// Find an active token by its jti claim.
        /// Called on every authenticated request by TokenValidationMiddleware.
        
        Task<UserToken?> GetByJwtIdAsync(string jwtId);

        ///Delete a specific token by jti. Called on logout.
        Task<bool> RevokeAsync(string jwtId);

       
        /// Delete ALL tokens for a user.
        /// Called before issuing a new token to enforce one-session-per-user.
      
        Task RevokeAllForUserAsync(int userId);

        /// <summary>Housekeeping — remove expired tokens.</summary>
        Task DeleteExpiredAsync();
    }
}