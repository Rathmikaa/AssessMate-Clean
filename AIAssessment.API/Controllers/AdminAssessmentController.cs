using AIAssessment.Application.DTOs.Assessment;
using AIAssessment.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIAssessment.API.Controllers;


// Admin CRUD for assessments.
// All endpoints require the "Admin" role — enforced by the [Authorize] attribute.
// The controller itself contains zero business logic.

[ApiController]
[Route("api/admin/assessments")]
[Authorize(Roles = "Admin")]
public class AdminAssessmentController : ControllerBase
{
    private readonly AssessmentService _assessmentService;

    public AdminAssessmentController(AssessmentService assessmentService)
    {
        _assessmentService = assessmentService;
    }

    // Get all assessments (active and inactive).
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _assessmentService.GetAllActiveAsync();
        return Ok(list);
    }

    //Get one assessment with its questions.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _assessmentService.GetByIdAsync(id);

        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    // Create a new assessment.
    [HttpPost]
    public async Task<IActionResult> Create(CreateAssessmentDto dto)
    {
        var result = await _assessmentService.CreateAsync(dto);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        // 201 Created with a Location header pointing to the new resource
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            result.Value);
    }

    ///  Update title, description, or duration 
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateAssessmentDto dto)
    {
        var result = await _assessmentService.UpdateAsync(id, dto);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return NoContent(); // 204 — success, no body
    }

    // Delete an assessment and all its questions
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _assessmentService.DeleteAsync(id);

        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        return NoContent();
    }
//Toggle IsActive on/off.
    [HttpPatch("{id:int}/toggle-active")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var result = await _assessmentService.ToggleActiveAsync(id);

        if (!result.IsSuccess)
             return NotFound(new { error = result.Error });

        return NoContent();
    }
}