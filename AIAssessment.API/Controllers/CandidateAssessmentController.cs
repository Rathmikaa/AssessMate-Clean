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
        /// <summary>
        /// gets a list of all active assessments available to candidates, allowing them to view and select assessments they can take.
        /// </summary>
        /// <returns></returns>

        [HttpGet]
        public async Task<IActionResult> GetAll()
            => ToResponse(await _assessmentService.GetAllActiveAsync());

        /// <summary>
        /// get detailed information about a specific assessment by its ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetForCandidate(int id)
            => ToResponse(await _assessmentService.GetForCandidateAsync(id));
    }
}