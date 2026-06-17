using AIAssessment.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIAssessment.API.Controllers;

[ApiController]
[Route("api/candidate/results")]
[Authorize(Roles = "Candidate")]
public class CandidateResultController : ControllerBase
{
    private readonly SubmissionService _submissionService;
     
    public CandidateResultController(SubmissionService submissionService)
    {
        _submissionService = submissionService;
    }

    //Get all past submission summaries for the logged-in candidate.
    [HttpGet]
    public async Task<IActionResult> GetMyResults()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var results = await _submissionService.GetMyResultsAsync(userId.Value);
        return Ok(results);
    }

   
    // Get the full breakdown of one submission.
    // The service enforces that candidates can only view their own submissions.
   
    [HttpGet("{submissionId:int}")]
    public async Task<IActionResult> GetDetail(int submissionId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _submissionService.GetDetailAsync(
            submissionId, requestingUserId: userId.Value, isAdmin: false);

        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }
}