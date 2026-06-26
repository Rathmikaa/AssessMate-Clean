using System.ComponentModel.DataAnnotations;

namespace AIAssessment.Application.DTOs.Auth
{
    /// <summary>
    /// Payload sent by a SuperAdmin to create a new Evaluator account.
    /// </summary>
    public class CreateEvaluatorDto
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = null!;
    }
}