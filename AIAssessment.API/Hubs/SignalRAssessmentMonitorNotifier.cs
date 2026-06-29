using AIAssessment.Application.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;

namespace AIAssessment.API.Hubs
{
    public class SignalRAssessmentMonitorNotifier : IAssessmentMonitorNotifier
    {
        private readonly IHubContext<AssessmentMonitorHub> _hub;
        public SignalRAssessmentMonitorNotifier(IHubContext<AssessmentMonitorHub> hub) => _hub = hub;

        public Task CandidateStartedAsync(int assessmentId, string assessmentTitle, int candidateId, string candidateName)
            => _hub.Clients.Group("Admins").SendAsync("CandidateStarted",
                new { assessmentId, assessmentTitle, candidateId, candidateName, at = DateTime.UtcNow });

        public Task CandidateSubmittedAsync(int assessmentId, string assessmentTitle, int candidateId, string candidateName, int score, int maxScore)
            => _hub.Clients.Group("Admins").SendAsync("CandidateSubmitted",
                new { assessmentId, assessmentTitle, candidateId, candidateName, score, maxScore, at = DateTime.UtcNow });
   
    
    }
}