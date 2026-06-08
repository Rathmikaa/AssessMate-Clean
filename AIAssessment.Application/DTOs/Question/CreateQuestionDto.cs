using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AIAssessment.Application.DTOs.Question
{
    public class CreateQuestionDto
    {
        [Required]
        public string QuestionText { get; set; } = null!;

        [Required]
        public string QuestionType { get; set; } = null!;
        
        [Range(1, 100)]
        public int MaxMarks { get; set; }

        public int AssessmentId { get; set; }

        // --- Descriptive only ---
        public string? ModelAnswer { get; set; }

        // --- MCQ only ---
        public List<string>? Options { get; set; }

        public int? CorrectOptionIndex { get; set; }



    }
}
