using AIAssessment.Application.Common;
using AIAssessment.Application.DTOs.Submission;
using AIAssessment.Application.Interfaces.Repositories;
using AIAssessment.Application.Interfaces.Services;
using AIAssessment.Domain.Entities;
using AIAssessment.Domain.Enums;
using AIAssessment.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AIAssessment.Application.Services
{
    public class SubmissionService
    {
        private readonly ISubmissionRepository _submissionRepo;
        private readonly IAssessmentRepository _assessmentRepo;
        private readonly IQuestionRepository _questionRepo;
        private readonly IScoringService _scoringService;
        private readonly UserManager<IdentityUser<int>> _userManager;
        private readonly IAssessmentMonitorNotifier _notifier;

        public SubmissionService(
            ISubmissionRepository submissionRepo,
            IAssessmentRepository assessmentRepo,
            IQuestionRepository questionRepo,
            IScoringService scoringService,
            UserManager<IdentityUser<int>> userManager,
            IAssessmentMonitorNotifier notifier)
        {
            _submissionRepo = submissionRepo;
            _assessmentRepo = assessmentRepo;
            _questionRepo = questionRepo;
            _scoringService = scoringService;
            _userManager = userManager;
            _notifier = notifier;
        }

        public async Task<Result> SubmitAsync(int userId, SubmitAssessmentDto dto)
        {
            var r = new Result();

            var assessment = await _assessmentRepo.GetByIdWithQuestionsAsync(dto.AssessmentId);
            if (assessment == null)
                return r.GetErrorResponse(404,
                    [$"Assessment with ID {dto.AssessmentId} was not found."]);

            if (!assessment.IsActive)
                return r.GetErrorResponse(400,
                    [$"Assessment '{assessment.Title}' is no longer active and cannot be submitted."]);

            var alreadySubmitted = await _submissionRepo.HasUserSubmittedAsync(
                userId, dto.AssessmentId);
            if (alreadySubmitted)
                return r.GetErrorResponse(409,
                    [$"You have already submitted '{assessment.Title}'. Multiple submissions are not allowed."]);

            try
            {
                var submission = Submission.Create(userId, assessment);
                var saved = await _submissionRepo.AddAsync(submission);

                int answeredCount = 0;
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
                        _ => throw new DomainException(
                            $"Unknown question type: {question.QuestionType}")
                    };

                    int score = await _scoringService.ScoreAnswerAsync(question, answer);
                    answer.SetScore(score);
                    saved.AddAnswer(answer);
                    answeredCount++;
                }

                saved.Submit();
                saved.Evaluate();
                await _submissionRepo.UpdateAsync(saved);

                int totalQuestions = assessment.Questions.Count;
                int maxPossible = assessment.Questions.Sum(q => q.MaxMarks);
                int percentage = maxPossible > 0
                    ? (int)Math.Round((double)saved.TotalScore / maxPossible * 100)
                    : 0;

                // NEW: live signal to any connected admin dashboards.
                var candidate = await _userManager.FindByIdAsync(userId.ToString());
                if (candidate != null)
                    await _notifier.CandidateSubmittedAsync(
                        assessment.Id, assessment.Title, userId, candidate.UserName ?? "Unknown",
                        saved.TotalScore, maxPossible);

                return r.GetResponse(new SubmissionResultDto
                {
                    SubmissionId = saved.Id,
                    AssessmentTitle = assessment.Title,
                    TotalScore = saved.TotalScore,
                    MaxPossibleScore = maxPossible,
                    Status = saved.Status.ToString(),
                    SubmittedAt = saved.SubmittedAt ?? DateTime.UtcNow
                }, 200, [
                    $"Assessment '{assessment.Title}' submitted successfully.",
                    $"You answered {answeredCount} of {totalQuestions} question(s).",
                    $"Your score: {saved.TotalScore} / {maxPossible} ({percentage}%)."
                ]);
            }
            catch (DomainException ex)
            {
                return r.GetErrorResponse(400, [ex.Message]);
            }
        }

        public async Task<Result> GetMyResultsAsync(int userId)
        {
            var r = new Result();
            var submissions = await _submissionRepo.GetByUserIdAsync(userId);
            // Own results — no need to look up the candidate's own name.
            var data = submissions.Select(s => MapToSummary(s, candidate: null)).ToList();

            return data.Count == 0
                ? r.GetResponse(data, 200, ["You have not submitted any assessments yet."])
                : r.GetResponse(data, 200, [$"You have {data.Count} submission(s)."]);
        }

        public async Task<Result> GetAllResultsAsync()
        {
            var r = new Result();
            var submissions = await _submissionRepo.GetAllAsync();

            // CHANGED: was MapToSummary(s, true) which only produced a "User #5"
            // placeholder. Now looks up the real candidate so admins see actual names.
            var data = new System.Collections.Generic.List<SubmissionSummaryDto>();
            foreach (var s in submissions)
            {
                var candidate = await _userManager.FindByIdAsync(s.UserId.ToString());
                data.Add(MapToSummary(s, candidate));
            }

            return data.Count == 0
                ? r.GetResponse(data, 200, ["No submissions found."])
                : r.GetResponse(data, 200, [$"{data.Count} total submission(s) found."]);
        }

        public async Task<Result> GetDetailAsync(
            int submissionId, int requestingUserId, bool isAdmin)
        {
            var r = new Result();

            var submission = await _submissionRepo.GetByIdWithDetailsAsync(submissionId);
            if (submission == null)
                return r.GetErrorResponse(404,
                    [$"Submission with ID {submissionId} was not found."]);

            if (!isAdmin && submission.UserId != requestingUserId)
                return r.GetErrorResponse(403,
                    ["You are not authorised to view this submission."]);

            int maxPossible = submission.Answers.Sum(a => a.Question.MaxMarks);
            int percentage = maxPossible > 0
                ? (int)Math.Round((double)submission.TotalScore / maxPossible * 100)
                : 0;

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

            return r.GetResponse(detail, 200, [
                $"Submission details for '{submission.Assessment.Title}'.",
                $"Final score: {submission.TotalScore} / {maxPossible} ({percentage}%).",
                $"Status: {submission.Status}."
            ]);
        }

        // CHANGED: was MapToSummary(Submission s, bool includeCandidate), which only
        // produced a "User #5" / "userId:5" placeholder. Now takes the actual
        // IdentityUser so admin views show the real name and email.
        private static SubmissionSummaryDto MapToSummary(Submission s, IdentityUser<int>? candidate) => new()
        {
            SubmissionId = s.Id,
            AssessmentTitle = s.Assessment?.Title ?? string.Empty,
            TotalScore = s.TotalScore,
            MaxPossibleScore = s.Assessment?.Questions.Sum(q => q.MaxMarks) ?? 0,
            Status = s.Status.ToString(),
            SubmittedAt = s.SubmittedAt ?? DateTime.UtcNow,
            CandidateName = candidate?.UserName,
            CandidateEmail = candidate?.Email
        };
    }
}