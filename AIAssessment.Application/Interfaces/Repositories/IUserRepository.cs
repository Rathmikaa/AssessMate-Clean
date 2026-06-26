using AIAssessment.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIAssessment.Application.Interfaces.Repositories
{ 
    public interface IUserRepository
    {
       Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<IEnumerable<User>> GetAllAsync();
        Task<bool> ExistsAsync(int id);

    }
}
