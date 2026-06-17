using System;
using System.Collections.Generic;
using System.Text;
using AIAssessment.Application.Interfaces.Repositories;
using AIAssessment.Application.DTOs.Assessment;
using AIAssessment.Domain.Entities;
using AIAssessment.Application.Common;
using AIAssessment.Domain.Exceptions;
using AIAssessment.Domain.Enums;
 

namespace AIAssessment.Application.Services
{
    //usecase : Admin -> create , update , delete ,activate/deactivete ,get all,get by id
    // candidate -> get all active ,get by id (questions without correct answers)
    public class AssessmentService
    {
        private readonly IAssessmentRepository _assessmentRepo;

        public AssessmentService(IAssessmentRepository assessmentRepo)
        {
            _assessmentRepo = assessmentRepo;
        }

        //Admin use case
        public async Task<Result<AssessmentSummaryDto>> CreateAsync(CreateAssessmentDto dto)
        {
            try
            {
                var assessment = Assessment.Create(dto.Title, dto.Description, dto.DurationMinutes);
                var saved = await _assessmentRepo.AddAsync(assessment);

                return Result<AssessmentSummaryDto>.Success(MapToSummary(saved));
            }
            catch(DomainException ex)
            {
                return Result<AssessmentSummaryDto>.Failure(ex.Message);
            }
        }


        public async Task<Result> UpdateAsync(int id, UpdateAssessmentDto dto)
        {
            var assessment = await _assessmentRepo.GetByIdAsync(id);
            if (assessment == null)
                return Result.Failure("Assessment not found");
            try
            {
                assessment.Update(dto.Title, dto.Description, dto.DurationMinutes);
                await _assessmentRepo.UpdateAsync(assessment);
                return Result.Success();

            }
            catch (DomainException ex)
            {
                return Result.Failure(ex.Message);
            }
        }

        public async Task<Result> DeleteAsync(int id)
        {
            var assessment = await _assessmentRepo.GetByIdAsync(id);
            if (assessment == null)
                return Result.Failure($"Assessment {id} not found.");

            await _assessmentRepo.DeleteAsync(assessment);
            return Result.Success();
        }
        public async Task<Result> ToggleActiveAsync(int id)
        {
            var assessment = await _assessmentRepo.GetByIdAsync(id);
            if (assessment == null)
                return Result.Failure($"Assessment {id} not found.");
            if (assessment.IsActive)
                assessment.Deactivate();
            else
                assessment.Activate();

            await _assessmentRepo.UpdateAsync(assessment);
            return Result.Success();
        }

        public async Task<Result<AssessmentDetailDto>> GetByIdAsync(int id) 
        {
            var assessment = await _assessmentRepo.GetByIdWithQuestionsAsync(id);
            if (assessment == null)
                return Result<AssessmentDetailDto>.Failure($"Assessment {id} not found.");
            return Result<AssessmentDetailDto>.Success(MapToDetail(assessment, includeCorrectAnswers: true));
        }

        //Candidate use case
        public async Task<IEnumerable<AssessmentSummaryDto>> GetAllActiveAsync()
        {
            var assessments = await _assessmentRepo.GetAllActiveAsync();
            return assessments.Select(MapToSummary);
        }

        public async Task<Result<AssessmentDetailDto>> GetForCandidateAsync(int id)
        {
            var assessment = await _assessmentRepo.GetByIdWithQuestionsAsync(id);
            if(assessment == null)
                return Result<AssessmentDetailDto>.Failure($"Assessment {id} not found.");
            if(!assessment.IsActive)
                return Result<AssessmentDetailDto>.Failure("This assessment is not currently available.");
            return Result<AssessmentDetailDto>.Success(MapToDetail(assessment, includeCorrectAnswers: false));
        }


        //Mapping Helpers

        private static AssessmentSummaryDto MapToSummary(Assessment a) => new AssessmentSummaryDto()
        {
            Id = a.Id,
            Title = a.Title,
            Description = a.Description,
            DurationMinutes = a.DurationMinutes,
            IsActive = a.IsActive,
            QuestionCount = a.Questions.Count
        };

        private static AssessmentDetailDto MapToDetail(Assessment a , bool includeCorrectAnswers)  => new AssessmentDetailDto()
        {
            Id = a.Id,
            Title = a.Title,
            Description = a.Description,
            DurationMinutes = a.DurationMinutes,
            IsActive =a.IsActive,
            CreatedAt = a.CreatedAt,
            Questions = a.Questions.Select(q => new QuestionInAssessmentDto()
            {
              Id = q.Id,
              QuestionText = q.QuestionText,
              QuestionType = q.QuestionType.ToString(),
              MaxMarks = q.MaxMarks,
              Options = q.QuestionType == Domain.Enums.QuestionType.MCQ?
                q.Options.Select(o => new OptionDto()
                {
                    Id = o.Id,
                    OptionText = o.OptionText,
                    
                }).ToList() : null
        
            }).ToList()
        };

        }
}
