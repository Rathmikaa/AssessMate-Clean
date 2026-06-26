
using System;

namespace AIAssessment.Domain.Entities
{
 
    /// A long-lived token used to obtain new access tokens without re-login.
    ///
    /// LIFECYCLE:
    ///   Login/Register → create RefreshToken (7 days) + AccessToken (60 min)
    ///   AccessToken expires → POST /api/auth/refresh with RefreshToken
    ///                       → old RefreshToken deleted, new pair issued
    ///   Logout → delete both RefreshToken and UserToken (access token)
    ///
    /// SECURITY:
    ///   - Stored as a cryptographically random string (not a JWT)
    ///   - Single use — each refresh rotates to a new token
    ///   - One per user — new login revokes any existing refresh token
    ///   - Expires after 7 days
    
    public class RefreshToken
    {
        public int Id { get; private set; }

        //FK to AspNetUsers.Id
        public int UserId { get; private set; }

        /// Cryptographically random string — NOT a JWT.
        /// The client stores this and sends it to /api/auth/refresh.
     
        public string Token { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime ExpiresAt { get; private set; }

        /// <summary>True until used or logged out.</summary>
        public bool IsActive => ExpiresAt > DateTime.UtcNow;

        protected RefreshToken()
        {
            Token = string.Empty;
        }

        public static RefreshToken Create(int userId, string token, int expiryDays = 7)
        {
            return new RefreshToken
            {
                UserId = userId,
                Token = token,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(expiryDays)
            };
        }
    }
}