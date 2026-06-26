using System.ComponentModel.DataAnnotations;

namespace AIAssessment.Application.DTOs.Auth
{
    //Client sends this to get a new access token.
    public class RefreshTokenDto
    {
        [Required]
        public string RefreshToken { get; set; } = null!;
    }
} 