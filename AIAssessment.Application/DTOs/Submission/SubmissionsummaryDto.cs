using System;
using System.Collections.Generic;
using System.Text;

namespace AIAssessment.Application.DTOs.Submission
{
    public class SubmissionSummaryDto
    {
        public int SubmissionId { get; set; }
        public string AssessmentTitle { get; set; } = null!;
        public int TotalScore { get; set; }
        public int MaxPossibleScore { get; set; }
        public string Status { get; set; } = null!;
        public DateTime SubmittedAt { get; set; }

        // Admin view only — candidate's identity
        public string? CandidateName { get; set; }
        public string? CandidateEmail { get; set; }
    }
}
