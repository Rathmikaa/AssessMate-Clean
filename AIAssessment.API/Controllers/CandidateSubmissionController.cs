using AIAssessment.Application.DTOs.Submission;
using AIAssessment.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIAssessment.API.Controllers;

[ApiController]
[Route("api/candidate/submissions")]
[Authorize(Roles = "Candidate")]
public class CandidateSubmissionController : ControllerBase
{
    private readonly SubmissionService _submissionService;

    public CandidateSubmissionController(SubmissionService submissionService)
    {
        _submissionService = submissionService;
    }

    
    /// Submit answers for an assessment.
    /// The candidate's identity comes from the JWT token — not from the request body.
    /// This prevents a candidate from submitting on behalf of someone else.
  
    [HttpPost]
    public async Task<IActionResult> Submit(SubmitAssessmentDto dto)
    {
        // Extract logged-in user's ID from the JWT claims
        // This was set in JwtTokenService as ClaimTypes.NameIdentifier
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { error = "Could not identify the logged-in user." });

        var result = await _submissionService.SubmitAsync(userId.Value, dto);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

   
    // Helper: extract the user ID from the JWT token claims
   
    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }
}