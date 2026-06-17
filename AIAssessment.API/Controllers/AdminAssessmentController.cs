using AIAssessment.Application.DTOs.Assessment;
using AIAssessment.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIAssessment.API.Controllers
{
    [Route("api/admin/assessments")]
    [Authorize(Roles = "Admin")]
    public class AdminAssessmentController : BaseController
    {
        private readonly AssessmentService _assessmentService;

        public AdminAssessmentController(AssessmentService assessmentService)
            => _assessmentService = assessmentService;

        /// <summary>
        /// get all assessment.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
            => ToResponse(await _assessmentService.GetAllAsync());
        /// <summary>
        /// get specific assessment.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
            => ToResponse(await _assessmentService.GetByIdAsync(id));
        /// <summary>
        /// create a new assessment
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Create(CreateAssessmentDto dto)
            => ToResponse(await _assessmentService.CreateAsync(dto));
        /// <summary>
        /// update  specific assessment
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateAssessmentDto dto)
            => ToResponse(await _assessmentService.UpdateAsync(id, dto));
        /// <summary>
        /// Delete an assessment.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
            => ToResponse(await _assessmentService.DeleteAsync(id));
        /// <summary>
        /// change assessment active status
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPatch("{id:int}/toggle-active")]
        public async Task<IActionResult> ToggleActive(int id)
            => ToResponse(await _assessmentService.ToggleActiveAsync(id));
    }
}