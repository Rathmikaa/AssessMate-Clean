using AIAssessment.Application.Interfaces.Repositories;
using AIAssessment.Domain.Entities;
using AIAssessment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIAssessment.Infrastructure.Persistence.Repositories;

public class QuestionRepository : IQuestionRepository
{
    private readonly AppDbContext _context;

    public QuestionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Question?> GetByIdAsync(int id)
        => await _context.Questions
            .FirstOrDefaultAsync(q => q.Id == id);

    public async Task<Question?> GetByIdWithOptionsAsync(int id)
        => await _context.Questions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);

    public async Task<IEnumerable<Question>> GetByAssessmentIdAsync(int assessmentId)
        => await _context.Questions
            .Where(q => q.AssessmentId == assessmentId)
            .Include(q => q.Options)
            .ToListAsync();

    public async Task<Question> AddAsync(Question question)
    {
        _context.Questions.Add(question);
        await _context.SaveChangesAsync();
        return question;
    }

    public async Task UpdateAsync(Question question)
    {
        _context.Questions.Update(question);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Question question)
    {
        _context.Questions.Remove(question);
        await _context.SaveChangesAsync();
    }
}