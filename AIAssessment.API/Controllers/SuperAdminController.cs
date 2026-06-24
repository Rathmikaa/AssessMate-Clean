// ── AIAssessment.API/Controllers/SuperAdminController.cs ─────────────────────
// NEW FILE — place next to AuthController.cs
// ─────────────────────────────────────────────────────────────────────────────

using AIAssessment.Application.Common;
using AIAssessment.Application.DTOs.Auth;
using AIAssessment.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIAssessment.API.Controllers
{
    /// <summary>
    /// Endpoints accessible only by the SuperAdmin role.
    /// SuperAdmin can create, list, and delete Evaluator accounts.
    /// </summary>
    [Route("api/superadmin")]
    [Authorize(Roles = Roles.SuperAdmin)]   // ← entire controller locked to SuperAdmin
    public class SuperAdminController : BaseController
    {
        private readonly EvaluatorService _evaluatorService;

        public SuperAdminController(EvaluatorService evaluatorService)
            => _evaluatorService = evaluatorService;

        /// <summary>
        /// Create a new Evaluator account.
        /// POST /api/superadmin/evaluators
        /// </summary>
        [HttpPost("evaluators")]
        public async Task<IActionResult> CreateEvaluator([FromBody] CreateEvaluatorDto dto)
            => ToResponse(await _evaluatorService.CreateEvaluatorAsync(dto));

        /// <summary>
        /// List all Evaluator accounts.
        /// GET /api/superadmin/evaluators
        /// </summary>
        [HttpGet("evaluators")]
        public async Task<IActionResult> GetEvaluators()
            => ToResponse(await _evaluatorService.GetEvaluatorsAsync());

        /// <summary>
        /// Delete an Evaluator account by ID.
        /// DELETE /api/superadmin/evaluators/{id}
        /// </summary>
        [HttpDelete("evaluators/{id:int}")]
        public async Task<IActionResult> DeleteEvaluator(int id)
            => ToResponse(await _evaluatorService.DeleteEvaluatorAsync(id));
    }
}