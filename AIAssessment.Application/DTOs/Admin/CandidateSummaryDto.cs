namespace AIAssessment.Application.DTOs.Admin
{
    public class CandidateSummaryDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public bool IsInvitePending { get; set; }   // hasn't set a password yet
        public bool IsActive { get; set; }          // false = deactivated by admin
        public DateTime CreatedAt { get; set; }
        public int SubmissionCount { get; set; }
    }
}