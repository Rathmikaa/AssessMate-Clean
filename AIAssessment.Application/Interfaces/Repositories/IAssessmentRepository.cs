using AIAssessment.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIAssessment.Application.Interfaces.Repositories
{ 
    public interface IAssessmentRepository
    {
        // Gets a Single Assessment by ID 
        Task<Assessment?> GetByIdAsync(int id);
        // Gets an assessment with its Questions and each question's Options loaded.
        Task<Assessment?> GetByIdWithQuestionsAsync(int id);
        Task<IEnumerable<Assessment>> GetAllAsync(); //Returns all assessments, including inactive ones
        Task<IEnumerable<Assessment>> GetAllActiveAsync(); //Returns all active assessments
        Task<Assessment> AddAsync(Assessment assessment); // 
        Task UpdateAsync (Assessment assessment);
        Task DeleteAsync(Assessment assessment);

        Task<bool> ExistsAsync(int id);


        
    }
}
