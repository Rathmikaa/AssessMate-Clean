using AIAssessment.Application.DTOs.Question;
using AIAssessment.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIAssessment.API.Controllers
{
    [Route("api/admin/questions")]
    [Authorize(Roles = "Evaluator")]

    public class AdminQuestionController : BaseController
    {
        private readonly QuestionService _questionService;

        public AdminQuestionController(QuestionService questionService)
            => _questionService = questionService;

        /// <summary>
        /// Admin can get questions by assessment ID to manage questions related to a specific assessment.
        /// </summary>
        /// <param name="assessmentId"></param>
        /// <returns></returns>

        [HttpGet("assessment/{assessmentId:int}")]
        public async Task<IActionResult> GetByAssessment(int assessmentId)
            => ToResponse(await _questionService.GetByAssessmentIdAsync(assessmentId));
        /// <summary>
        /// Admin can create a new question for an assessment. 
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>

        [HttpPost]
        public async Task<IActionResult> Create(CreateQuestionDto dto)
            => ToResponse(await _questionService.CreateAsync(dto));

        /// <summary>
        /// update an existing question. 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dto"></param>
        /// <returns></returns>

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, CreateQuestionDto dto)
            => ToResponse(await _questionService.UpdateAsync(id, dto));
        /// <summary>
        /// Admin can delete a question by its ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>


        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
            => ToResponse(await _questionService.DeleteAsync(id));
    }
}