using AIAssessment.Application.Interfaces.Repositories;
using AIAssessment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AIAssessment.Infrastructure.Persistence.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDbContext _db;
         
        public RefreshTokenRepository(AppDbContext db) => _db = db;

        public async Task<RefreshToken> AddAsync(RefreshToken token)
        {
            _db.RefreshTokens.Add(token);
            await _db.SaveChangesAsync();
            return token;
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token)
            => await _db.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == token);

        public async Task<bool> RevokeAsync(string token)
        {
            var stored = await _db.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == token);

            if (stored == null) return false;

            _db.RefreshTokens.Remove(stored);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task RevokeAllForUserAsync(int userId)
        {
            var tokens = await _db.RefreshTokens
                .Where(t => t.UserId == userId)
                .ToListAsync();

            if (tokens.Any())
            {
                _db.RefreshTokens.RemoveRange(tokens);
                await _db.SaveChangesAsync();
            }
        }

        public async Task DeleteExpiredAsync()
        {
            var expired = await _db.RefreshTokens
                .Where(t => t.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            if (expired.Any())
            {
                _db.RefreshTokens.RemoveRange(expired);
                await _db.SaveChangesAsync();
            }
        }
    }
}