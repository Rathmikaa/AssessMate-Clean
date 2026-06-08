using AIAssessment.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace AIAssessment.API.Controllers;

[ApiController]
[Route("api/admin/results")]
[Authorize(Roles = "Admin")]
public class AdminResultController : ControllerBase
{
    private readonly SubmissionService _submissionService;
    private readonly UserManager<IdentityUser<int>> _userManager;

    public AdminResultController(
        SubmissionService submissionService,
        UserManager<IdentityUser<int>> userManager)
    {
        _submissionService = submissionService;
        _userManager = userManager;
    }

    /// All submissions with candidate email resolved from Identity.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var results = await _submissionService.GetAllResultsAsync();

        // Enrich with real email from AspNetUsers
        var enriched = new System.Collections.Generic.List<object>();
        foreach (var r in results)
        {
            // UserId was stored as "userId:123" — parse the number back
            var userId = r.CandidateEmail?.Replace("userId:", "") ?? "";
            var user = await _userManager.FindByIdAsync(userId);

            enriched.Add(new
            {
                r.SubmissionId,
                r.AssessmentTitle,
                r.TotalScore,
                r.MaxPossibleScore,
                r.Status,
                r.SubmittedAt,
                CandidateEmail = user?.Email ?? "Unknown",
            });
        }

        return Ok(enriched);
    }

    /// Full breakdown of one submission.
    [HttpGet("{submissionId:int}")]
    public async Task<IActionResult> GetDetail(int submissionId)
    {
        var result = await _submissionService.GetDetailAsync(
            submissionId, requestingUserId: 0, isAdmin: true);

        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }
}