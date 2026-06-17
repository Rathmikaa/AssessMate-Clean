using AIAssessment.Application.Interfaces.Repositories;
using AIAssessment.Domain.Entities;
using AIAssessment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIAssessment.Infrastructure.Persistence.Repositories;



// This class is the ONLY place in the codebase that writes raw EF Core
// queries for assessments. If you ever switch from SQL Server to PostgreSQL,
// you change this file — nothing in Application or Domain needs to change.
 
public class AssessmentRepository : IAssessmentRepository
{
    private readonly AppDbContext _context;

    public AssessmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Assessment?> GetByIdAsync(int id)
        => await _context.Assessments
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<Assessment?> GetByIdWithQuestionsAsync(int id)
        => await _context.Assessments
            .Include(a => a.Questions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<IEnumerable<Assessment>> GetAllAsync()
        => await _context.Assessments
            .Include(a => a.Questions)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<Assessment>> GetAllActiveAsync()
        => await _context.Assessments
            .Where(a => a.IsActive)
            .Include(a => a.Questions)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

    public async Task<Assessment> AddAsync(Assessment assessment)
    {
        _context.Assessments.Add(assessment);
        await _context.SaveChangesAsync();
        return assessment;
    }

    public async Task UpdateAsync(Assessment assessment)
    {
        _context.Assessments.Update(assessment);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Assessment assessment)
    {
        _context.Assessments.Remove(assessment);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id)
        => await _context.Assessments.AnyAsync(a => a.Id == id);
}