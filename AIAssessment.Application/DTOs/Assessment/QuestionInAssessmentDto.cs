using System;
using System.Collections.Generic;
using System.Text;

namespace AIAssessment.Application.DTOs.Assessment
{
    public class QuestionInAssessmentDto
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = null!;
        public string QuestionType { get; set; } = null!;
        public int MaxMarks { get; set; }

        // Only populated for MCQ questions — candidates must not see IsCorrect
        public List<OptionDto>? Options { get; set; }
    }
}
