namespace AIAssessment.Application.Interfaces.Services
{
    public interface IAssessmentMonitorNotifier
    {
        Task CandidateStartedAsync(int assessmentId, string assessmentTitle, int candidateId, string candidateName);
        Task CandidateSubmittedAsync(int assessmentId, string assessmentTitle, int candidateId, string candidateName, int score, int maxScore);
    }
}