using System;
using System.Collections.Generic;
using System.Text;

namespace AIAssessment.Application.DTOs.Submission
{
    public class AnswerDetailDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = null!;
        public string QuestionType { get; set; } = null!;
        public int MaxMarks { get; set; }

        public string? UserAnswer { get; set; }
        public string? CorrectAnswer { get; set; }
        public int Score { get; set; }
    } 
}
