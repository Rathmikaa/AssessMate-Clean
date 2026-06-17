using System;
using System.Collections.Generic;
using System.Text;

namespace AIAssessment.Application.DTOs.Question
{ 
    public class QuestionResponseDto
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = null!;
        public string QuestionType { get; set; } = null!;
        public int MaxMarks { get; set; }
        public string? ModelAnswer { get; set; }
        public List<OptionResponseDto> Options { get; set; } = new();
    }
}
