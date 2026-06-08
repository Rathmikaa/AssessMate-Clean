using AIAssessment.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIAssessment.Application.Interfaces.Repositories
{
    public interface IQuestionRepository
    {
        Task<Question?> GetByIdAsync(int id);
        Task<Question> GetByIdWithOptionsAsync(int id);
        Task<IEnumerable<Question>> GetByAssessmentIdAsync(int assessmentId);

        Task<Question> AddAsync(Question question);
        Task UpdateAsync(Question question);
        Task DeleteAsync(Question question);
    }
}
