using System;
using System.Collections.Generic;
using System.Text;
using AIAssessment.Domain.Enums;
using AIAssessment.Domain.Exceptions;

namespace AIAssessment.Domain.Entities
{
    public class Answer
    {
        public int Id { get; private set; }
        public int QuestionId { get; private set; }
        public int SubmissionId { get; private set; }
        public int? SelectedOptionId { get; private set; } // For MCQ
        public string? AnswerText { get; private set; } // For descriptive

        public int Score { get; private set; }
        public Submission Submission { get; private set; } = null!;
        public Question Question { get; private set; } = null!;

        // private constructor for EF Core
        protected Answer()
        {
        }
        //Factory method 

        public static Answer ForMcq(int submissionId, int questionId, int selectedOptionId)
        {
           
            return new Answer
            {
                SubmissionId = submissionId,
                QuestionId = questionId,
                SelectedOptionId = selectedOptionId,
                Score = 0
            };
        }

        public static Answer ForDescriptive(int submissionId, int questionId, string answerText)
        {
            if (string.IsNullOrWhiteSpace(answerText))
                throw new DomainException("Answer text is required for descriptive questions.", nameof(answerText));
            return new Answer
            {
                SubmissionId = submissionId,
                QuestionId = questionId,
                AnswerText = answerText.Trim(),
                Score = 0
            };
        }

        //Behavior method 
        public void SetScore(int score)
        {
            if (score < 0)
                throw new DomainException("Score cannot be negative.");

            Score = score;
        }
    }
}
