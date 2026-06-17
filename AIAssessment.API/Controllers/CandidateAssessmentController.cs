using AIAssessment.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            => ToResponse(await _assessmentService.GetForCandidateAsync(id));
    }
}