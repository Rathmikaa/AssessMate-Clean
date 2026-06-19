using AIAssessment.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AIAssessment.API.Controllers
{
    [Route("api/admin/results")]
    [Authorize(Roles = "Admin")]
    public class AdminResultController : BaseController
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
        /// <summary>
        /// Gets all submission results for all assessments. 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
            => ToResponse(await _submissionService.GetAllResultsAsync());

        /// <summary>
        /// Get specific submission result by submissionId.
        /// </summary>
        /// <param name="submissionId"></param>
        /// <returns></returns>


        [HttpGet("{submissionId:int}")]
        public async Task<IActionResult> GetDetail(int submissionId)
            => ToResponse(await _submissionService.GetDetailAsync(
                submissionId, requestingUserId: 0, isAdmin: true));
    }
}