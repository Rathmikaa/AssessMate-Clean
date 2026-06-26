using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AIAssessment.Application.DTOs.Submission
{
    public class SubmitAssessmentDto
    {
        [Required]
        public int AssessmentId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one answer is required.")]
        public List<AnswerDto> Answers { get; set; } = new();
    }
}
 