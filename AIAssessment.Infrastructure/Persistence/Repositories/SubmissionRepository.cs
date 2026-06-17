using AIAssessment.Application.Interfaces.Repositories;
using AIAssessment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIAssessment.Infrastructure.Persistence.Repositories
{
    public class SubmissionRepository : ISubmissionRepository
    {
        private readonly AppDbContext _context;

        public SubmissionRepository(AppDbContext context) => _context = context;

        public async Task<Submission?> GetByIdAsync(int id)
            => await _context.Submissions
                .FirstOrDefaultAsync(s => s.Id == id);

        public async Task<Submission?> GetByIdWithDetailsAsync(int id)
            => await _context.Submissions
                .Include(s => s.Assessment)
                    .ThenInclude(a => a.Questions)
                        .ThenInclude(q => q.Options)
                .Include(s => s.Answers)
                    .ThenInclude(a => a.Question)
                        .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(s => s.Id == id);

        public async Task<IEnumerable<Submission>> GetByUserIdAsync(int userId)
            => await _context.Submissions
                .Where(s => s.UserId == userId)
                .Include(s => s.Assessment)
                    .ThenInclude(a => a.Questions) //  needed for MaxPossibleScore
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();

        public async Task<IEnumerable<Submission>> GetByAssessmentIdAsync(int assessmentId)
            => await _context.Submissions
                .Where(s => s.AssessmentId == assessmentId)
                .Include(s => s.Assessment)
                    .ThenInclude(a => a.Questions)
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();

        public async Task<IEnumerable<Submission>> GetAllAsync()
            => await _context.Submissions
                .Include(s => s.Assessment)
                    .ThenInclude(a => a.Questions) //  needed for MaxPossibleScore
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();

        public async Task<bool> HasUserSubmittedAsync(int userId, int assessmentId)
            => await _context.Submissions
                .AnyAsync(s => s.UserId == userId
                            && s.AssessmentId == assessmentId);

        public async Task<Submission> AddAsync(Submission submission)
        {
            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();
            return submission;
        }

        public async Task UpdateAsync(Submission submission)
        {
            _context.Submissions.Update(submission);
            await _context.SaveChangesAsync();
        }
    }
}