using AIAssessment.Application.Common;
using AIAssessment.Application.DTOs.Submission;
using AIAssessment.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIAssessment.API.Controllers
{
    [Route("api/candidate/submissions")]
    [Authorize(Roles = "Candidate")]
    public class CandidateSubmissionController : BaseController
    {
        private readonly SubmissionService _submissionService;

        public CandidateSubmissionController(SubmissionService submissionService)
            => _submissionService = submissionService;

        [HttpPost]
        public async Task<IActionResult> Submit(SubmitAssessmentDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                var r = new Result();
                return ToResponse(r.GetErrorResponse(401, ["Could not identify the logged-in user."]));
            }

            return ToResponse(await _submissionService.SubmitAsync(userId.Value, dto));
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : null;
        }
    }
}