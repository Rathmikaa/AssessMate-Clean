using AIAssessment.Application.Common;
using AIAssessment.Application.DTOs.Admin;
using AIAssessment.Application.DTOs.Submission;
using AIAssessment.Application.Interfaces.Repositories;
using AIAssessment.Application.Interfaces.Services;
using AIAssessment.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AIAssessment.Application.Services
{
    public class CandidateService
    {
        private readonly UserManager<IdentityUser<int>> _userManager;
        private readonly IAssessmentRepository _assessmentRepo;
        private readonly ISubmissionRepository _submissionRepo;
        private readonly IUserTokenRepository _tokenRepo;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly ILogger<CandidateService> _logger;

        public CandidateService(
            UserManager<IdentityUser<int>> userManager,
            IAssessmentRepository assessmentRepo,
            ISubmissionRepository submissionRepo,
            IUserTokenRepository tokenRepo,
            IEmailService emailService,
            IConfiguration config,
            ILogger<CandidateService> logger)
        {
            _userManager = userManager;
            _assessmentRepo = assessmentRepo;
            _submissionRepo = submissionRepo;
            _tokenRepo = tokenRepo;
            _emailService = emailService;
            _config = config;
            _logger = logger;
        }

        public async Task<Result> CreateAsync(CreateCandidateDto dto)
        {
            var r = new Result();

            if (await _userManager.FindByEmailAsync(dto.Email) != null)
                return r.GetErrorResponse(409, [$"An account with '{dto.Email}' already exists."]);

            Assessment? assessment = null;
            if (dto.AssessmentId.HasValue)
            {
                assessment = await _assessmentRepo.GetByIdAsync(dto.AssessmentId.Value);
                if (assessment == null)
                    return r.GetErrorResponse(404, [$"Assessment with ID {dto.AssessmentId} was not found."]);
            }

            var candidate = new IdentityUser<int>
            {
                UserName = dto.FullName,
                Email = dto.Email,
                EmailConfirmed = true
            };

            // No password — candidate sets their own via the invite link.
            var createResult = await _userManager.CreateAsync(candidate);
            if (!createResult.Succeeded)
                return r.GetErrorResponse(400, createResult.Errors.Select(e => e.Description).ToList());

            await _userManager.AddToRoleAsync(candidate, "Candidate");

            var messages = new List<string> { $"Candidate '{candidate.UserName}' created successfully." };
            await SendSetupInviteAsync(candidate, assessment, messages);

            return r.GetResponse(MapToSummary(candidate, isInvitePending: true, submissionCount: 0), 201, messages);
        }

        public async Task<Result> GetAllAsync()
        {
            var r = new Result();
            var candidates = await _userManager.GetUsersInRoleAsync("Candidate");

            var data = new List<CandidateSummaryDto>();
            foreach (var c in candidates)
            {
                var hasPassword = await _userManager.HasPasswordAsync(c);
                var count = await _submissionRepo.CountByUserIdAsync(c.Id);
                data.Add(MapToSummary(c, isInvitePending: !hasPassword, submissionCount: count));
            }

            return data.Count == 0
                ? r.GetResponse(data, 200, ["No candidates found."])
                : r.GetResponse(data, 200, [$"{data.Count} candidate(s) found."]);
        }

        public async Task<Result> GetDetailAsync(int id)
        {
            var r = new Result();
            var candidate = await _userManager.FindByIdAsync(id.ToString());
            if (candidate == null)
                return r.GetErrorResponse(404, [$"Candidate with ID {id} was not found."]);

            var hasPassword = await _userManager.HasPasswordAsync(candidate);
            var submissions = await _submissionRepo.GetByUserIdAsync(id);
            var submissionDtos = submissions.Select(s => new SubmissionSummaryDto
            {
                SubmissionId = s.Id,
                AssessmentTitle = s.Assessment?.Title ?? string.Empty,
                TotalScore = s.TotalScore,
                MaxPossibleScore = s.Assessment?.Questions.Sum(q => q.MaxMarks) ?? 0,
                Status = s.Status.ToString(),
                SubmittedAt = s.SubmittedAt ?? DateTime.UtcNow
            }).ToList();

            var detail = new CandidateDetailDto
            {
                Id = candidate.Id,
                FullName = candidate.UserName!,
                Email = candidate.Email!,
                IsInvitePending = !hasPassword,
                IsActive = IsActive(candidate),
                SubmissionCount = submissionDtos.Count,
                Submissions = submissionDtos
            };

            return r.GetResponse(detail, 200, [$"Candidate details for '{candidate.UserName}'."]);
        }

        public async Task<Result> ResendInviteAsync(int id, int? assessmentId)
        {
            var r = new Result();
            var candidate = await _userManager.FindByIdAsync(id.ToString());
            if (candidate == null)
                return r.GetErrorResponse(404, [$"Candidate with ID {id} was not found."]);

            if (await _userManager.HasPasswordAsync(candidate))
                return r.GetErrorResponse(400, ["This candidate has already set up their account."]);

            Assessment? assessment = null;
            if (assessmentId.HasValue)
            {
                assessment = await _assessmentRepo.GetByIdAsync(assessmentId.Value);
                if (assessment == null)
                    return r.GetErrorResponse(404, [$"Assessment with ID {assessmentId} was not found."]);
            }

            var messages = new List<string>();
            await SendSetupInviteAsync(candidate, assessment, messages);
            return r.GetResponse(null, 200, messages);
        }

        public async Task<Result> InviteToAssessmentAsync(int id, int assessmentId)
        {
            var r = new Result();
            var candidate = await _userManager.FindByIdAsync(id.ToString());
            if (candidate == null)
                return r.GetErrorResponse(404, [$"Candidate with ID {id} was not found."]);

            var assessment = await _assessmentRepo.GetByIdAsync(assessmentId);
            if (assessment == null)
                return r.GetErrorResponse(404, [$"Assessment with ID {assessmentId} was not found."]);

            if (!await _userManager.HasPasswordAsync(candidate))
                return r.GetErrorResponse(400, ["This candidate hasn't set up their account yet. Use 'resend invite' instead."]);

            var baseUrl = _config["Frontend:BaseUrl"] ?? "http://localhost:4200";
            var link = $"{baseUrl}/assessment/{assessment.Id}";

            try
            {
                await _emailService.SendAssessmentInviteAsync(candidate.Email!, candidate.UserName!, assessment.Title, link);
                return r.GetResponse(null, 200, [$"Invite for '{assessment.Title}' sent to {candidate.Email}."]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send assessment invite to {Email}", candidate.Email);
                return r.GetErrorResponse(502, ["Candidate exists, but the invite email failed to send. Please try again."]);
            }
        }

        public async Task<Result> DeactivateAsync(int id)
        {
            var r = new Result();
            var candidate = await _userManager.FindByIdAsync(id.ToString());
            if (candidate == null)
                return r.GetErrorResponse(404, [$"Candidate with ID {id} was not found."]);

            await _userManager.SetLockoutEnabledAsync(candidate, true);
            await _userManager.SetLockoutEndDateAsync(candidate, DateTimeOffset.MaxValue);
            await _tokenRepo.RevokeAllForUserAsync(candidate.Id); // kick any active session immediately

            return r.GetResponse(null, 200, [$"Candidate '{candidate.UserName}' has been deactivated."]);
        }

        public async Task<Result> ReactivateAsync(int id)
        {
            var r = new Result();
            var candidate = await _userManager.FindByIdAsync(id.ToString());
            if (candidate == null)
                return r.GetErrorResponse(404, [$"Candidate with ID {id} was not found."]);

            await _userManager.SetLockoutEndDateAsync(candidate, null);
            return r.GetResponse(null, 200, [$"Candidate '{candidate.UserName}' has been reactivated."]);
        }

        private async Task SendSetupInviteAsync(IdentityUser<int> candidate, Assessment? assessment, List<string> messages)
        {
            try
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(candidate);
                var baseUrl = _config["Frontend:BaseUrl"] ?? "http://localhost:4200";
                var link = $"{baseUrl}/set-password?email={Uri.EscapeDataString(candidate.Email!)}&token={Uri.EscapeDataString(token)}"
                    + (assessment != null ? $"&assessmentId={assessment.Id}" : string.Empty);

                await _emailService.SendCandidateSetupInviteAsync(candidate.Email!, candidate.UserName!, link, assessment?.Title);
                messages.Add($"Invite email sent to {candidate.Email}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send invite email to {Email}", candidate.Email);
                messages.Add("Candidate saved, but the invite email failed to send. Use 'resend invite' to try again.");
            }
        }

        private static bool IsActive(IdentityUser<int> c) => c.LockoutEnd == null || c.LockoutEnd < DateTimeOffset.UtcNow;

        private static CandidateSummaryDto MapToSummary(IdentityUser<int> c, bool isInvitePending, int submissionCount) => new()
        {
            Id = c.Id,
            FullName = c.UserName!,
            Email = c.Email!,
            IsInvitePending = isInvitePending,
            IsActive = IsActive(c),
            SubmissionCount = submissionCount
        };
    }
}