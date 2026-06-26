using AIAssessment.Application.Common;
using AIAssessment.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIAssessment.API.Controllers
{
    [Route("api/candidate/assessments")]
    [Authorize(Roles = "Candidate")]
    public class CandidateAssessmentController : BaseController
    {
        private readonly AssessmentService _assessmentService;

        public CandidateAssessmentController(AssessmentService assessmentService)
            => _assessmentService = assessmentService;

        [HttpGet]
        public async Task<IActionResult> GetAll()
            => ToResponse(await _assessmentService.GetAllActiveAsync());

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetForCandidate(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                var r = new Result();
                return ToResponse(r.GetErrorResponse(401, ["Could not identify the logged-in user."]));
            }

            return ToResponse(await _assessmentService.GetForCandidateAsync(id, userId.Value));
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : null;
        }
    }
}