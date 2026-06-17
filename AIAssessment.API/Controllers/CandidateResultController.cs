using AIAssessment.Application.Common;
using AIAssessment.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIAssessment.API.Controllers
{
    [Route("api/candidate/results")]
    [Authorize(Roles = "Candidate")]
    public class CandidateResultController : BaseController
    {
        private readonly SubmissionService _submissionService;

        public CandidateResultController(SubmissionService submissionService)
            => _submissionService = submissionService;

        [HttpGet]
        public async Task<IActionResult> GetMyResults()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                var r = new Result();
                return ToResponse(r.GetErrorResponse(401, ["Could not identify the logged-in user."]));
            }

            return ToResponse(await _submissionService.GetMyResultsAsync(userId.Value));
        }

        [HttpGet("{submissionId:int}")]
        public async Task<IActionResult> GetDetail(int submissionId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                var r = new Result();
                return ToResponse(r.GetErrorResponse(401, ["Could not identify the logged-in user."]));
            }

            return ToResponse(await _submissionService.GetDetailAsync(
                submissionId, requestingUserId: userId.Value, isAdmin: false));
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : null;
        }
    }
}