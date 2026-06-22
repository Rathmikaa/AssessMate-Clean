using AIAssessment.Application.Interfaces.Repositories;
using AIAssessment.Domain.Entities;
using AIAssessment.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
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

        // used by CandidateService's admin candidate list/monitor view.
        public async Task<int> CountByUserIdAsync(int userId)
            => await _context.Submissions.CountAsync(s => s.UserId == userId);

        public async Task<Submission> AddAsync(Submission submission)
        {
            try
            {
                _context.Submissions.Add(submission);
                await _context.SaveChangesAsync();
                return submission;
            }
            catch (DbUpdateException ex) when (IsDuplicateSubmission(ex))
            {
                // Defense-in-depth: SubmissionService already checks
                // HasUserSubmittedAsync before calling this, but two near-simultaneous
                // requests from the same candidate can both pass that check before
                // either insert commits. The unique index on (UserId, AssessmentId)
                // in SubmissionConfiguration is what actually stops the second insert;
                // this just turns the raw SQL exception into a clean, expected message
                // that SubmissionService's existing `catch (DomainException ex)` block
                // already knows how to turn into a 400 response.
                throw new DomainException(
                    "You have already submitted this assessment. Multiple submissions are not allowed.");
            }
        }

        public async Task UpdateAsync(Submission submission)
        {
            _context.Submissions.Update(submission);
            await _context.SaveChangesAsync();
        }

        private static bool IsDuplicateSubmission(DbUpdateException ex)
            => ex.InnerException?.Message.Contains(
                "IX_Submissions_UserId_AssessmentId", StringComparison.OrdinalIgnoreCase) == true;
    }
}