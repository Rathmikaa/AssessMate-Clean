using System;
using System.Collections.Generic;
using System.Text;

namespace AIAssessment.Application.DTOs.Assessment
{ 
    public class AssessmentSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int DurationMinutes { get; set; }
        public bool IsActive { get; set; }
        public int QuestionCount { get; set; }
      

    }
}
