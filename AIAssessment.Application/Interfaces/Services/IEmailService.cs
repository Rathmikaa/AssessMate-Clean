namespace AIAssessment.Application.Interfaces.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink);
        Task SendCandidateSetupInviteAsync(string toEmail, string toName, string setupLink, string? assessmentTitle);
        Task SendAssessmentInviteAsync(string toEmail, string toName, string assessmentTitle, string assessmentLink);
    }
}