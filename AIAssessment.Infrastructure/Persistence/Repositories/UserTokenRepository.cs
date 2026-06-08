using AIAssessment.Application.Interfaces.Repositories;
using AIAssessment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AIAssessment.Infrastructure.Persistence.Repositories
{
    public class UserTokenRepository : IUserTokenRepository
    {
        private readonly AppDbContext _db;

        public UserTokenRepository(AppDbContext db) => _db = db;

        public async Task<UserToken> AddAsync(UserToken token)
        {
            _db.UserTokens.Add(token);
            await _db.SaveChangesAsync();
            return token;
        }

        public async Task<UserToken?> GetByJwtIdAsync(string jwtId)
            => await _db.UserTokens
                .FirstOrDefaultAsync(t => t.JwtId == jwtId);

        public async Task<bool> RevokeAsync(string jwtId)
        {
            // Delete the row entirely — no need to keep revoked tokens around.
            // This keeps the table small and lookups fast.
            var token = await _db.UserTokens
                .FirstOrDefaultAsync(t => t.JwtId == jwtId);

            if (token == null) return false;

            _db.UserTokens.Remove(token);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task RevokeAllForUserAsync(int userId)
        {
            var tokens = await _db.UserTokens
                .Where(t => t.UserId == userId)
                .ToListAsync();

            if (tokens.Any())
            {
                _db.UserTokens.RemoveRange(tokens);
                await _db.SaveChangesAsync();
            }
        }

        public async Task DeleteExpiredAsync()
        {
            var expired = await _db.UserTokens
                .Where(t => t.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            if (expired.Any())
            {
                _db.UserTokens.RemoveRange(expired);
                await _db.SaveChangesAsync();
            }
        }
    }
}