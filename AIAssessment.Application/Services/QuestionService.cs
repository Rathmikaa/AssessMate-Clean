using AIAssessment.Application.Common;
using AIAssessment.Application.DTOs.Question;
using AIAssessment.Application.Interfaces.Repositories;
using AIAssessment.Domain.Entities;
using AIAssessment.Domain.Enums;
using AIAssessment.Domain.Exceptions;
using System.Linq;
using System.Threading.Tasks;

namespace AIAssessment.Application.Services
{
    public class QuestionService
    {
        private readonly IQuestionRepository _questionRepo;
        private readonly IAssessmentRepository _assessmentRepo;

        public QuestionService(
            IQuestionRepository questionRepo,
            IAssessmentRepository assessmentRepo)
        {
            _questionRepo = questionRepo;
            _assessmentRepo = assessmentRepo;
        }

        public async Task<Result> CreateAsync(CreateQuestionDto dto)
        {
            var r = new Result();

            var assessment = await _assessmentRepo.GetByIdAsync(dto.AssessmentId);
            if (assessment == null)
                return r.GetErrorResponse(404,
                    [$"Assessment with ID {dto.AssessmentId} was not found."]);

            if (!System.Enum.TryParse<QuestionType>(dto.QuestionType, ignoreCase: true, out var questionType))
                return r.GetErrorResponse(400,
                    [$"Invalid question type '{dto.QuestionType}'. Must be 'MCQ' or 'Descriptive'."]);

            try
            {
                var question = questionType == QuestionType.MCQ
                    ? BuildMcqQuestion(dto)
                    : BuildDescriptiveQuestion(dto);

                var saved = await _questionRepo.AddAsync(question);
                return r.GetResponse(MapToResponse(saved), 201,
                    [$"{questionType} question added to assessment '{assessment.Title}'."]);
            }
            catch (DomainException ex)
            {
                return r.GetErrorResponse(400, [ex.Message]);
            }
        }

        public async Task<Result> UpdateAsync(int id, CreateQuestionDto dto)
        {
            var r = new Result();

            var question = await _questionRepo.GetByIdAsync(id);
            if (question == null)
                return r.GetErrorResponse(404,
                    [$"Question with ID {id} was not found."]);

            if (!System.Enum.TryParse<QuestionType>(dto.QuestionType, ignoreCase: true, out var questionType))
                return r.GetErrorResponse(400,
                    [$"Invalid question type '{dto.QuestionType}'. Must be 'MCQ' or 'Descriptive'."]);

            try
            {
                question.UpdateText(dto.QuestionText);
                if (questionType == QuestionType.Descriptive && dto.ModelAnswer != null)
                    question.UpdateModelAnswer(dto.ModelAnswer);
                if (questionType == QuestionType.MCQ)
                {
                    question.ClearOptions();
                    AddOptionsToQuestion(question, dto);
                    question.ValidateMcq();
                }
                await _questionRepo.UpdateAsync(question);
                return r.GetResponse(MapToResponse(question), 200,
                    [$"Question ID {id} updated successfully."]);
            }
            catch (DomainException ex)
            {
                return r.GetErrorResponse(400, [ex.Message]);
            }
        }

        public async Task<Result> DeleteAsync(int id)
        {
            var r = new Result();
            var question = await _questionRepo.GetByIdAsync(id);
            if (question == null)
                return r.GetErrorResponse(404,
                    [$"Question with ID {id} was not found."]);

            await _questionRepo.DeleteAsync(question);
            return r.GetResponse(null, 200,
                [$"Question ID {id} deleted successfully."]);
        }

        public async Task<Result> GetByAssessmentIdAsync(int assessmentId)
        {
            var r = new Result();
            var assessment = await _assessmentRepo.GetByIdAsync(assessmentId);
            if (assessment == null)
                return r.GetErrorResponse(404,
                    [$"Assessment with ID {assessmentId} was not found."]);

            var questions = await _questionRepo.GetByAssessmentIdAsync(assessmentId);
            var data = questions.Select(MapToResponse).ToList();

            return data.Count == 0
                ? r.GetResponse(data, 200,
                    [$"Assessment '{assessment.Title}' has no questions yet."])
                : r.GetResponse(data, 200,
                    [$"{data.Count} question(s) found for assessment '{assessment.Title}'."]);
        }

        // ── Private helpers ───────────────────────────────────────────────────

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
                dto.QuestionText, dto.MaxMarks, dto.ModelAnswer, dto.AssessmentId);
        }

        private static void AddOptionsToQuestion(Question question, CreateQuestionDto dto)
        {
            for (int i = 0; i < dto.Options!.Count; i++)
                question.AddOption(Option.Create(dto.Options[i], isCorrect: i == dto.CorrectOptionIndex));
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