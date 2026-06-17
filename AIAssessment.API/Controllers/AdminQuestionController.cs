using AIAssessment.Application.DTOs.Question;
using AIAssessment.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIAssessment.API.Controllers;

[ApiController]
[Route("api/admin/questions")]
[Authorize(Roles = "Admin")]
public class AdminQuestionController : ControllerBase
{
    private readonly QuestionService _questionService;
     
    public AdminQuestionController(QuestionService questionService)
    {
        _questionService = questionService;
    }

    // Get all questions for an assessment (with options and correct answers).
    [HttpGet("assessment/{assessmentId:int}")]
    public async Task<IActionResult> GetByAssessment(int assessmentId)
    {
        var result = await _questionService.GetByAssessmentIdAsync(assessmentId);

        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    // Add a question (MCQ or Descriptive) to an assessment.
    [HttpPost]
    public async Task<IActionResult> Create(CreateQuestionDto dto)
    {
        var result = await _questionService.CreateAsync(dto);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    // Update a question's text, marks, or options.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CreateQuestionDto dto)
    {
        var result = await _questionService.UpdateAsync(id, dto);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    // Delete a question permanently.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _questionService.DeleteAsync(id);

        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        return Ok(" Question Deleted Successfully");
    }
}