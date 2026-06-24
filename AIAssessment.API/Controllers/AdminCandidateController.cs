using AIAssessment.Application.DTOs.Admin;
using AIAssessment.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIAssessment.API.Controllers
{
    [Route("api/admin/candidates")]
    [Authorize(Roles = "Evaluator")]
    public class AdminCandidateController : BaseController
    {
        private readonly CandidateService _candidateService;
        public AdminCandidateController(CandidateService candidateService) => _candidateService = candidateService;

        /// <summary>Creates a candidate account. If AssessmentId is set, the invite links straight to it.</summary>
        [HttpPost]
        public async Task<IActionResult> Create(CreateCandidateDto dto)
            => ToResponse(await _candidateService.CreateAsync(dto));

        /// <summary>Lists all candidates with invite/active status and submission counts — the monitor view.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
            => ToResponse(await _candidateService.GetAllAsync());

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
            => ToResponse(await _candidateService.GetDetailAsync(id));

        [HttpPost("{id:int}/resend-invite")]
        public async Task<IActionResult> ResendInvite(int id, ResendInviteDto dto)
            => ToResponse(await _candidateService.ResendInviteAsync(id, dto.AssessmentId));

        [HttpPost("{id:int}/invite/{assessmentId:int}")]
        public async Task<IActionResult> InviteToAssessment(int id, int assessmentId)
            => ToResponse(await _candidateService.InviteToAssessmentAsync(id, assessmentId));

        [HttpPatch("{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int id)
            => ToResponse(await _candidateService.DeactivateAsync(id));

        [HttpPatch("{id:int}/reactivate")]
        public async Task<IActionResult> Reactivate(int id)
            => ToResponse(await _candidateService.ReactivateAsync(id));
    }
}