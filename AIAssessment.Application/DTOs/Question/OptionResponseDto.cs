using System;
using System.Collections.Generic;
using System.Text;

namespace AIAssessment.Application.DTOs.Question
{
    public class OptionResponseDto
    {
        public int Id { get; set; }
        public string OptionText { get; set; } = null!;
        public bool IsCorrect { get; set; }
    }
}
