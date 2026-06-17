using AIAssessment.Application.Common;
using AIAssessment.Application.DTOs.Submission;
using AIAssessment.Application.Interfaces.Repositories;
using AIAssessment.Application.Interfaces.Services;
using AIAssessment.Domain.Entities;
using AIAssessment.Domain.Enums;
using AIAssessment.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIAssessment.Application.Services
{
    public class SubmissionService
    {
        private readonly  ISubmissionRepository _submissionRepo;
        private readonly IAssessmentRepository _assessmentRepo;
        private readonly IQuestionRepository _questionRepo;
        private readonly IScoringService _scoringService;

        public SubmissionService(
            ISubmissionRepository submissionRepo,
            IAssessmentRepository assessmentRepo,
            IQuestionRepository questionRepo,
            IScoringService scoringService)
        {
            _submissionRepo = submissionRepo;
            _assessmentRepo = assessmentRepo;
            _questionRepo = questionRepo;
            _scoringService = scoringService;
        }

        //  Submit 
        public async Task<Result<SubmissionResultDto>> SubmitAsync(int userId, SubmitAssessmentDto dto)
        {
            var assessment = await _assessmentRepo.GetByIdWithQuestionsAsync(dto.AssessmentId);
            if (assessment == null)
                return Result<SubmissionResultDto>.Failure(
                    $"Assessment {dto.AssessmentId} not found.");

            var alreadySubmitted = await _submissionRepo.HasUserSubmittedAsync(userId, dto.AssessmentId);
            if (alreadySubmitted)
                return Result<SubmissionResultDto>.Failure(
                    "You have already submitted this assessment.");

            try
            {
                var submission = Submission.Create(userId, assessment);
                var saved = await _submissionRepo.AddAsync(submission);

                foreach (var answerDto in dto.Answers)
                {
                    var question = assessment.Questions
                        .FirstOrDefault(q => q.Id == answerDto.QuestionId);

                    if (question == null) continue;

                    Answer answer = question.QuestionType switch
                    {
                        QuestionType.MCQ => Answer.ForMcq(
                            saved.Id, question.Id, answerDto.SelectedOptionId ?? 0),
                        QuestionType.Descriptive => Answer.ForDescriptive(
                            saved.Id, question.Id, answerDto.AnswerText ?? string.Empty),
                        _ => throw new DomainException($"Unknown question type: {question.QuestionType}")
                    };

                    int score = await _scoringService.ScoreAnswerAsync(question, answer);
                    answer.SetScore(score);
                    saved.AddAnswer(answer);
                }

                saved.Submit();
                saved.Evaluate();
                await _submissionRepo.UpdateAsync(saved);

                int maxPossible = assessment.Questions.Sum(q => q.MaxMarks);

                return Result<SubmissionResultDto>.Success(new SubmissionResultDto
                {
                    SubmissionId = saved.Id,
                    AssessmentTitle = assessment.Title,
                    TotalScore = saved.TotalScore,
                    MaxPossibleScore = maxPossible,
                    Status = saved.Status.ToString(),
                    SubmittedAt = saved.SubmittedAt ?? DateTime.UtcNow
                });
            }
            catch (DomainException e)
            {
                return Result<SubmissionResultDto>.Failure(e.Message);
            }
        }

        //  Candidate: my results 
        public async Task<IEnumerable<SubmissionSummaryDto>> GetMyResultsAsync(int userId)
        {
            var submissions = await _submissionRepo.GetByUserIdAsync(userId);
            return submissions.Select(s => MapToSummary(s, includeCandidate: false));
        }

        // Admin: all results
        public async Task<IEnumerable<SubmissionSummaryDto>> GetAllResultsAsync()
        {
            var submissions = await _submissionRepo.GetAllAsync();
            return submissions.Select(s => MapToSummary(s, includeCandidate: true));
        }

        // ── Detail view ───────────────────────────────────────────────────────
        public async Task<Result<SubmissionDetailDto>> GetDetailAsync(
            int submissionId, int requestingUserId, bool isAdmin)
        {
            var submission = await _submissionRepo.GetByIdWithDetailsAsync(submissionId);
            if (submission == null)
                return Result<SubmissionDetailDto>.Failure(
                    $"Submission {submissionId} not found.");

            if (!isAdmin && submission.UserId != requestingUserId)
                return Result<SubmissionDetailDto>.Failure(
                    "You are not authorised to view this submission.");

            int maxPossible = submission.Answers.Sum(a => a.Question.MaxMarks);

            var detail = new SubmissionDetailDto
            {
                SubmissionId = submission.Id,
                AssessmentTitle = submission.Assessment.Title,
                TotalScore = submission.TotalScore,
                MaxPossibleScore = maxPossible,
                Status = submission.Status.ToString(),
                SubmittedAt = submission.SubmittedAt ?? DateTime.UtcNow,
                Answers = submission.Answers.Select(a => new AnswerDetailDto
                {
                    QuestionId = a.QuestionId,
                    QuestionText = a.Question.QuestionText,
                    QuestionType = a.Question.QuestionType.ToString(),
                    MaxMarks = a.Question.MaxMarks,
                    CorrectAnswer = a.Question.QuestionType == QuestionType.MCQ
                        ? a.Question.Options.FirstOrDefault(o => o.IsCorrect)?.OptionText
                        : a.Question.ModelAnswer,
                    UserAnswer = a.Question.QuestionType == QuestionType.MCQ
                        ? a.Question.Options.FirstOrDefault(o => o.Id == a.SelectedOptionId)?.OptionText
                        : a.AnswerText,
                    Score = a.Score
                }).ToList()
            };

            return Result<SubmissionDetailDto>.Success(detail);
        }

        // ── Private helpers ───────────────────────────────────────────────────
        private static SubmissionSummaryDto MapToSummary(Submission s, bool includeCandidate) => new()
        {
            SubmissionId = s.Id,
            AssessmentTitle = s.Assessment?.Title ?? string.Empty,
            TotalScore = s.TotalScore,
            MaxPossibleScore = s.Assessment?.Questions.Sum(q => q.MaxMarks) ?? 0,
            Status = s.Status.ToString(),
            SubmittedAt = s.SubmittedAt ?? DateTime.UtcNow,
            // User navigation is gone — show UserId as a string for admin view.
            // To show email/name, you'd join with UserManager in the service.
            CandidateName = includeCandidate ? $"User #{s.UserId}" : null,
            CandidateEmail = includeCandidate ? $"userId:{s.UserId}" : null
        };
    }
}