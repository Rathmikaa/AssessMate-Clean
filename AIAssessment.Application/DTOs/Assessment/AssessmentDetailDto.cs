using System;
using System.Collections.Generic;
using System.Text;

namespace AIAssessment.Application.DTOs.Assessment
{
    public class AssessmentDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int DurationMinutes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<QuestionInAssessmentDto> Questions { get; set; } = new();
    }
}
