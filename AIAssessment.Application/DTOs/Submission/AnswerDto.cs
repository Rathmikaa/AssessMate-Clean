using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AIAssessment.Application.DTOs.Submission
{
    public class AnswerDto
    {
        [Required]
        public int QuestionId { get; set; }

        // For MCQ questions
        public int? SelectedOptionId { get; set; }

        // For Descriptive questions
        public string? AnswerText { get; set; }
    }
}
