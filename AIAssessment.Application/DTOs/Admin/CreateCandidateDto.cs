using System.ComponentModel.DataAnnotations;

namespace AIAssessment.Application.DTOs.Admin
{
    public class CreateCandidateDto
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; } = null!;

        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        /// Optional — if set, the invite links straight into this assessment.
        public int? AssessmentId { get; set; }
    }
}