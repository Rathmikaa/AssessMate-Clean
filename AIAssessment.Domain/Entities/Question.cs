using System;
using System.Collections.Generic;
using System.Text;
using AIAssessment.Domain.Enums;
using AIAssessment.Domain.Exceptions;


namespace AIAssessment.Domain.Entities
{
    public class Question
    {
        public int Id { get; private set; }
        public string QuestionText { get; private set; }
        
        public QuestionType QuestionType { get; private set; }
        public int MaxMarks { get; private set; }

        //for descriptive question

        public string? ModelAnswer { get; private set; }
        //Foreign key
        public int AssessmentId { get; private set; }
        // Navigation
        public Assessment Assessment { get; private set; } = null!;

        protected readonly List<Option> _options = new();
        public IReadOnlyCollection<Option> Options => _options.AsReadOnly();

        //private constructor for EF Core
        protected Question()
        {
            QuestionText = string.Empty;
        }

        //Factory method for creating a new question

        public static Question CreateMcq(string questionText , int maxMarks , int assessmentId)
        {
            ValidateCommon(questionText, maxMarks);

            return new Question
            {
                QuestionText = questionText,
                QuestionType = QuestionType.MCQ,
                MaxMarks = maxMarks,
                AssessmentId = assessmentId
            };
        }

        // create descriptive question
        public static Question CreateDescriptive(string questionText, int maxMarks, string modelAnswer, int assessmentId)
        {
            ValidateCommon(questionText, maxMarks);
            if (string.IsNullOrWhiteSpace(modelAnswer))
                throw new DomainException("Descriptive questions must have a model answer.");
           return new Question
            {
                QuestionText = questionText.Trim(),
                QuestionType = QuestionType.Descriptive,
                MaxMarks = maxMarks,
                ModelAnswer = modelAnswer.Trim(),
                AssessmentId = assessmentId
            };
        }

        //Behaviour methods

        public void AddOption(Option option)
        {
            if(QuestionType != QuestionType.MCQ)
                throw new DomainException("Options can only be added to MCQ questions.");
            if(option.IsCorrect && _options.Any(o => o.IsCorrect))
                throw new DomainException("An MCQ question can only have one correct option.");
            option.SetQuestionId(Id);
            _options.Add(option);
        }

        public void ValidateMcq()
        {
            if (QuestionType != QuestionType.MCQ) return;

            if(_options.Count < 2)
                throw new DomainException("An MCQ question must have at least two options.");
            if(!_options.Any(o => o.IsCorrect))
                throw new DomainException("MCQ questions must have exactly one correct option.");
        
        }

        public void UpdateText(string newText)
        {
            if (string.IsNullOrWhiteSpace(newText))
                throw new DomainException("Question text cannot be empty.");
            QuestionText = newText.Trim();
        }

        public void UpdateModelAnswer(string modelAnswer)
        {
            if(QuestionType != QuestionType.Descriptive)
                throw new DomainException("Model answer is only applicable to Descriptive questions.");
            if (string.IsNullOrWhiteSpace(modelAnswer))
                throw new DomainException("Model answer cannot be empty.");
            ModelAnswer = modelAnswer.Trim();
        }

        public void ClearOptions()
        {
            _options.Clear();
        }
        //private helper 

        private static void ValidateCommon(string questionText, int maxMarks)
        {
            if (string.IsNullOrWhiteSpace(questionText))
                throw new DomainException("Question text is required.", nameof(questionText));
            if (maxMarks <= 0)
                throw new DomainException("Max marks must be greater than zero.", nameof(maxMarks));
        }

    }
}
