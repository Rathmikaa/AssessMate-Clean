using System;

namespace AIAssessment.Domain.Entities
{
    
    /// Stores an issued JWT in the database.
    ///
    /// FLOW:
    ///   Login  → generate JWT → save row here → return token to client
    ///   Request → [Authorize] validates signature → TokenValidationMiddleware
    ///             checks row exists here → allow or 401
    ///   Logout  → delete row → token immediately invalid even if JWT not expired
    ///
    /// ONE TOKEN PER USER:
    ///   On every login we call RevokeAllForUserAsync first, then insert one new row.
    ///   The unique index on UserId in the DB enforces this at database level too.
    
    public class UserToken
    {
        public int Id { get; private set; }

        /// FK to AspNetUsers.Id (Identity user's int PK)
        public int UserId { get; private set; }

        /// The full JWT string returned to the client.
        public string Token { get; private set; }

        /// The 'jti' claim embedded in the JWT — a short unique ID.
        /// Indexed because TokenValidationMiddleware queries it on every request.
  
        public string JwtId { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime ExpiresAt { get; private set; }

        protected UserToken()
        {
            Token = string.Empty;
            JwtId = string.Empty;
        }

        public static UserToken Create(int userId, string token, string jwtId, DateTime expiresAt)
        {
            return new UserToken
            {
                UserId = userId,
                Token = token,
                JwtId = jwtId,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}