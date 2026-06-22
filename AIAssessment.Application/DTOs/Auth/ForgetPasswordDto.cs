using System.ComponentModel.DataAnnotations;

namespace AIAssessment.Application.DTOs.Auth
{
    public class ForgotPasswordDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;
    }
}