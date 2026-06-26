using AIAssessment.Application.DTOs.Submission;

namespace AIAssessment.Application.DTOs.Admin
{
    public class CandidateDetailDto : CandidateSummaryDto
    {
        public List<SubmissionSummaryDto> Submissions { get; set; } = new();
    }
}