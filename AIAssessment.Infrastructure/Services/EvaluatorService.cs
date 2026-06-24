using AIAssessment.Application.Common;
using AIAssessment.Application.DTOs.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AIAssessment.Infrastructure.Services
{
    public class EvaluatorService
    {
        private readonly UserManager<IdentityUser<int>> _userManager;
        private readonly ILogger<EvaluatorService> _logger;

        public EvaluatorService(
            UserManager<IdentityUser<int>> userManager,
            ILogger<EvaluatorService> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        //Create a new Evaluator
        public async Task<Result> CreateEvaluatorAsync(CreateEvaluatorDto dto)
        {
            var result = new Result();

            // Duplicate email check
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null)
                return result.GetErrorResponse(400, [$"Email '{dto.Email}' is already registered."]);

            var user = new IdentityUser<int>
            {
                UserName = dto.FullName,
                Email = dto.Email,
                NormalizedEmail = dto.Email.ToUpperInvariant(),
                NormalizedUserName = dto.FullName.ToUpperInvariant(),
                EmailConfirmed = true   // SuperAdmin creates verified accounts
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
            {
                var errors = createResult.Errors.Select(e => e.Description).ToList();
                _logger.LogWarning("Evaluator creation failed for {Email}: {Errors}",
                    dto.Email, string.Join(", ", errors));
                return result.GetErrorResponse(400, errors);
            }

            await _userManager.AddToRoleAsync(user, Roles.Evaluator);
            _logger.LogInformation("Evaluator '{Email}' created by SuperAdmin.", dto.Email);

            return result.GetResponse(new
            {
                user.Id,
                user.Email,
                user.UserName,
                Role = Roles.Evaluator
            }, 201);
        }

        // ── List all Evaluators ───────────────────────────────────────────────
        public async Task<Result> GetEvaluatorsAsync()
        {
            var result = new Result();
            var evaluators = await _userManager.GetUsersInRoleAsync(Roles.Evaluator);

            var data = evaluators.Select(u => new
            {
                u.Id,
                FullName = u.UserName,
                u.Email
            });

            return result.GetResponse(data, 200);
        }

        // ── Delete / deactivate an Evaluator ─────────────────────────────────
        public async Task<Result> DeleteEvaluatorAsync(int evaluatorId)
        {
            var result = new Result();

            var user = await _userManager.FindByIdAsync(evaluatorId.ToString());
            if (user == null)
                return result.GetErrorResponse(404, ["Evaluator not found."]);

            // Safety: prevent deleting a SuperAdmin via this endpoint
            if (await _userManager.IsInRoleAsync(user, Roles.SuperAdmin))
                return result.GetErrorResponse(403, ["Cannot delete a SuperAdmin through this endpoint."]);

            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
                return result.GetErrorResponse(400,
                    deleteResult.Errors.Select(e => e.Description).ToList());

            return result.GetResponse("Evaluator deleted successfully.", 200);
        }
    }
}