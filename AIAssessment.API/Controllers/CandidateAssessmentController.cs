using AIAssessment.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIAssessment.API.Controllers;

[ApiController]
[Route("api/candidate/assessments")]
[Authorize(Roles = "Candidate")]
public class CandidateAssessmentController : ControllerBase
{
    private readonly AssessmentService _assessmentService;
     
    public CandidateAssessmentController(AssessmentService assessmentService)
    {
        _assessmentService = assessmentService;
    }

    // Browse all active assessments available to take.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _assessmentService.GetAllActiveAsync();
        return Ok(list);
    }

   
    // Get one assessment with its questions — ready to take.
    // Options are returned WITHOUT IsCorrect (that's hidden from candidates).
  
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetForCandidate(int id)
    {
        var result = await _assessmentService.GetForCandidateAsync(id);

        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }
}