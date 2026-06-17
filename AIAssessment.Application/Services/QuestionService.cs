using System;
using System.Collections.Generic;
using System.Text;
using AIAssessment.Domain.Entities;
using AIAssessment.Domain.Enums;
using AIAssessment.Domain.Exceptions;
using AIAssessment.Application.Interfaces.Repositories;
using AIAssessment.Application.DTOs.Question;
using AIAssessment.Application.Common;

namespace AIAssessment.Application.Services
{
    // usecase : Admin -> create , update , delete ,activate/deactivete ,get all,get by id
    public class QuestionService
    {
        private readonly  IQuestionRepository _questionRepo;
        private readonly IAssessmentRepository _assessmentRepo;

        public QuestionService(IQuestionRepository questionRepo, IAssessmentRepository assessmentRepo)
        {
            _questionRepo = questionRepo;
            _assessmentRepo = assessmentRepo;
        }
        public async Task<Result<QuestionResponseDto>> CreateAsync(CreateQuestionDto dto)
        {
            var assessment = await _assessmentRepo.GetByIdAsync(dto.AssessmentId);

            if(assessment == null)
              return Result<QuestionResponseDto>.Failure($"Assessment {dto.AssessmentId}not found");
            if(!Enum.TryParse<QuestionType>(dto.QuestionType,ignoreCase : true,out var questionType ))
                return Result<QuestionResponseDto>.Failure("Question Type must be 'MCQ' or 'Descriptive'");

            try
            {
                Question question;
                if(questionType == QuestionType.MCQ)
                {
                    question = BuildMcqQuestion(dto);
                }
                else
                {
                    question = BuildDescriptiveQuestion(dto);
                }
                var saved = await _questionRepo.AddAsync(question);
                return Result<QuestionResponseDto>.Success(MapToResponse(saved));
                
            }
            catch(DomainException e)
            {
                return Result<QuestionResponseDto>.Failure(e.Message);

            }

        }
        public async Task<Result<QuestionResponseDto>> UpdateAsync(int  id ,CreateQuestionDto dto)
        {
            var question = await _questionRepo.GetByIdAsync(id);
            if(question == null)
                return Result<QuestionResponseDto>.Failure($"Question {id} not found");
            if(!Enum.TryParse<QuestionType>(dto.QuestionType, ignoreCase: true, out var questionType))
                return Result<QuestionResponseDto>.Failure("Question Type must be 'MCQ' or 'Descriptive'");

            try
            {
                question.UpdateText(dto.QuestionText);
                if (questionType == QuestionType.Descriptive && dto.ModelAnswer != null)
                    question.UpdateModelAnswer(dto.ModelAnswer);

                if(questionType == QuestionType.MCQ)
                {
                    question.ClearOptions();
                    AddOptionsToQuestion(question, dto);
                    question.ValidateMcq();
                }
                await _questionRepo.UpdateAsync(question);

                return Result<QuestionResponseDto>.Success(MapToResponse(question));

            }
            catch(DomainException e)
            {
                return Result<QuestionResponseDto>.Failure(e.Message);
            }
        }

        public async Task<Result> DeleteAsync(int id)
        {
            var question = await _questionRepo.GetByIdAsync(id);
            if (question == null)
                return Result.Failure($"Question {id} not found");
            await _questionRepo.DeleteAsync(question);
            return Result.Success();


        }
        public async Task<Result<IEnumerable<QuestionResponseDto>>> GetByAssessmentIdAsync(int assessmentId)
        {
            var exists = await _assessmentRepo.ExistsAsync(assessmentId);
            if (!exists)
                return Result<IEnumerable<QuestionResponseDto>>.Failure(
                    $"Assessment {assessmentId} not found.");

            var questions = await _questionRepo.GetByAssessmentIdAsync(assessmentId);
            return Result<IEnumerable<QuestionResponseDto>>.Success(
                questions.Select(MapToResponse));
        }

        //private helper 

        private static Question BuildMcqQuestion(CreateQuestionDto dto)
        {
            if (dto.Options == null || dto.Options.Count < 2)
                throw new DomainException("MCQ questions require at least 2 options.");

            if (dto.CorrectOptionIndex == null)
                throw new DomainException("MCQ questions require a CorrectOptionIndex.");

            if (dto.CorrectOptionIndex < 0 || dto.CorrectOptionIndex >= dto.Options.Count)
                throw new DomainException(
                    $"CorrectOptionIndex must be between 0 and {dto.Options.Count - 1}.");

            var question = Question.CreateMcq(dto.QuestionText, dto.MaxMarks, dto.AssessmentId);
            AddOptionsToQuestion(question, dto);
            question.ValidateMcq();
            return question;
        }
        private static Question BuildDescriptiveQuestion(CreateQuestionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ModelAnswer))
                throw new DomainException("Descriptive questions require a ModelAnswer.");

            return Question.CreateDescriptive(
                dto.QuestionText, dto.MaxMarks,  dto.ModelAnswer, dto.AssessmentId);
        }

        private static void AddOptionsToQuestion(Question question, CreateQuestionDto dto)
        {
            for (int i = 0; i < dto.Options!.Count; i++)
            {
                var option = Option.Create(dto.Options[i], isCorrect: i == dto.CorrectOptionIndex);
                question.AddOption(option);
            }
        }

        private static QuestionResponseDto MapToResponse(Question q) => new()
        {
            Id = q.Id,
            QuestionText = q.QuestionText,
            QuestionType = q.QuestionType.ToString(),
            MaxMarks = q.MaxMarks,
            ModelAnswer = q.ModelAnswer,
            Options = q.Options.Select(o => new OptionResponseDto
            {
                Id = o.Id,
                OptionText = o.OptionText,
                IsCorrect = o.IsCorrect
            }).ToList()
        };
    }

}
