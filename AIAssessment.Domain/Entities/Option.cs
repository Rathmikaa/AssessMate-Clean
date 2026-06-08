using System;
using System.Collections.Generic;
using System.Text;
using AIAssessment.Domain.Exceptions;


namespace AIAssessment.Domain.Entities
{
    public class Option
    {
        public int Id { get; private set; }

        public string OptionText { get; private set; }
        public bool IsCorrect { get; private set; }

        //foreign key
        public int QuestionId { get; private set; }
        // Navigation
        public Question Question { get; private set; } = null!;
        //private constructor for EF Core
        protected Option() 
        {
            OptionText = string.Empty;
        }

        //Factory method 

        public static Option Create(string optionText, bool isCorrect)
        {
            if (string.IsNullOrWhiteSpace(optionText))
                throw new DomainException("Option text is required.", nameof(optionText));
            return new Option
            {
                OptionText = optionText.Trim(),
                IsCorrect = isCorrect
            };
        }

        public void SetQuestionId(int questionId)
        {
            QuestionId = questionId;
        }


    }
}
