using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AIAssessment.Application.DTOs.Assessment
{
    public class CreateAssessmentDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        [Range(1, 480)] // max 8-hour assessment
        public int DurationMinutes { get; set; }
    }
}
