using AIAssessment.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIAssessment.Application.Interfaces.Repositories
{
    public interface ISubmissionRepository
    {
        Task<Submission?> GetByIdAsync(int id);
        Task<Submission?> GetByIdWithDetailsAsync(int id);
        Task<IEnumerable<Submission>> GetByUserIdAsync(int userId);
        Task<IEnumerable<Submission>> GetByAssessmentIdAsync(int assessmentId);
        Task<IEnumerable<Submission>> GetAllAsync();
        Task<int> CountByUserIdAsync(int userId);
        // Checks if a candidate has already submitted this assessment
        // Used to prevent duplicate submissions.
        Task<bool> HasUserSubmittedAsync(int userId, int assessmentId);

        Task<Submission> AddAsync(Submission submission);
        Task UpdateAsync(Submission submission);
    }
}
