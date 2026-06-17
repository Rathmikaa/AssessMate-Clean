using AIAssessment.Application.Common;
using AIAssessment.Application.DTOs.Assessment;
using AIAssessment.Application.Interfaces.Repositories;
using AIAssessment.Domain.Entities;
using AIAssessment.Domain.Exceptions;
using System.Linq;
using System.Threading.Tasks;

namespace AIAssessment.Application.Services
{
    public class AssessmentService
    {
        private readonly IAssessmentRepository _assessmentRepo;

        public AssessmentService(IAssessmentRepository assessmentRepo)
            => _assessmentRepo = assessmentRepo;

        public async Task<Result> CreateAsync(CreateAssessmentDto dto)
        {
            var r = new Result();
            try
            {
                var assessment = Assessment.Create(dto.Title, dto.Description, dto.DurationMinutes);
                var saved = await _assessmentRepo.AddAsync(assessment);
                return r.GetResponse(MapToSummary(saved), 201,
                    [$"Assessment '{saved.Title}' created successfully."]);
            }
            catch (DomainException ex)
            {
                return r.GetErrorResponse(400, [ex.Message]);
            }
        }

        public async Task<Result> UpdateAsync(int id, UpdateAssessmentDto dto)
        {
            var r = new Result();
            var assessment = await _assessmentRepo.GetByIdAsync(id);
            if (assessment == null)
                return r.GetErrorResponse(404,
                    [$"Assessment with ID {id} was not found."]);
            try
            {
                assessment.Update(dto.Title, dto.Description, dto.DurationMinutes);
                await _assessmentRepo.UpdateAsync(assessment);
                return r.GetResponse(null, 200,
                    [$"Assessment '{assessment.Title}' updated successfully."]);
            }
            catch (DomainException ex)
            {
                return r.GetErrorResponse(400, [ex.Message]);
            }
        }

        public async Task<Result> DeleteAsync(int id)
        {
            var r = new Result();
            var assessment = await _assessmentRepo.GetByIdAsync(id);
            if (assessment == null)
                return r.GetErrorResponse(404,
                    [$"Assessment with ID {id} was not found."]);

            await _assessmentRepo.DeleteAsync(assessment);
            return r.GetResponse(null, 200,
                [$"Assessment '{assessment.Title}' deleted successfully."]);
        }

        public async Task<Result> ToggleActiveAsync(int id)
        {
            var r = new Result();
            var assessment = await _assessmentRepo.GetByIdAsync(id);
            if (assessment == null)
                return r.GetErrorResponse(404,
                    [$"Assessment with ID {id} was not found."]);

            if (assessment.IsActive) assessment.Deactivate();
            else assessment.Activate();

            await _assessmentRepo.UpdateAsync(assessment);

            var status = assessment.IsActive ? "activated" : "deactivated";
            return r.GetResponse(null, 200,
                [$"Assessment '{assessment.Title}' has been {status}."]);
        }

        public async Task<Result> GetAllAsync()
        {
            var r = new Result();
            var list = await _assessmentRepo.GetAllAsync();
            var data = list.Select(MapToSummary).ToList();

            return data.Count == 0
                ? r.GetResponse(data, 200, ["No assessments found."])
                : r.GetResponse(data, 200, [$"{data.Count} assessment(s) found."]);
        }

        public async Task<Result> GetByIdAsync(int id)
        {
            var r = new Result();
            var assessment = await _assessmentRepo.GetByIdWithQuestionsAsync(id);
            if (assessment == null)
                return r.GetErrorResponse(404,
                    [$"Assessment with ID {id} was not found."]);

            return r.GetResponse(MapToDetail(assessment, includeCorrectAnswers: true), 200,
                [$"Assessment '{assessment.Title}' retrieved successfully."]);
        }

        public async Task<Result> GetAllActiveAsync()
        {
            var r = new Result();
            var list = await _assessmentRepo.GetAllActiveAsync();
            var data = list.Select(MapToSummary).ToList();

            return data.Count == 0
                ? r.GetResponse(data, 200, ["No active assessments available at this time."])
                : r.GetResponse(data, 200, [$"{data.Count} active assessment(s) available."]);
        }

        public async Task<Result> GetForCandidateAsync(int id)
        {
            var r = new Result();
            var assessment = await _assessmentRepo.GetByIdWithQuestionsAsync(id);
            if (assessment == null)
                return r.GetErrorResponse(404,
                    [$"Assessment with ID {id} was not found."]);
            if (!assessment.IsActive)
                return r.GetErrorResponse(400,
                    [$"Assessment '{assessment.Title}' is not currently available."]);

            return r.GetResponse(MapToDetail(assessment, includeCorrectAnswers: false), 200,
                [$"Assessment '{assessment.Title}' loaded. You have {assessment.DurationMinutes} minutes."]);
        }

        // ── Mapping helpers ───────────────────────────────────────────────────

        private static AssessmentSummaryDto MapToSummary(Assessment a) => new()
        {
            Id = a.Id,
            Title = a.Title,
            Description = a.Description,
            DurationMinutes = a.DurationMinutes,
            IsActive = a.IsActive,
            QuestionCount = a.Questions.Count
        };

        private static AssessmentDetailDto MapToDetail(Assessment a, bool includeCorrectAnswers) => new()
        {
            Id = a.Id,
            Title = a.Title,
            Description = a.Description,
            DurationMinutes = a.DurationMinutes,
            IsActive = a.IsActive,
            CreatedAt = a.CreatedAt,
            Questions = a.Questions.Select(q => new QuestionInAssessmentDto
            {
                Id = q.Id,
                QuestionText = q.QuestionText,
                QuestionType = q.QuestionType.ToString(),
                MaxMarks = q.MaxMarks,
                Options = q.QuestionType == Domain.Enums.QuestionType.MCQ
                    ? q.Options.Select(o => new OptionDto
                    {
                        Id = o.Id,
                        OptionText = o.OptionText
                    }).ToList()
                    : null
            }).ToList()
        };
    }
}