using System;
using System.Collections.Generic;
using System.Text;
using AIAssessment.Domain.Enums;
using AIAssessment.Domain.Exceptions;

namespace AIAssessment.Domain.Entities
{
    public class Submission
    {
        public int Id { get; private set; }
        public int UserId { get; private set; }
        public int AssessmentId { get; private set; }
        public SubmissionStatus Status { get; private set; }

        public int TotalScore { get; private set; }

        public DateTime StartedAt { get; private set; }
        public DateTime? SubmittedAt { get; private set; }

        //Navigation properties

        public User User { get; private set; } = null!;
        public Assessment Assessment { get; private set; } = null!;

        protected readonly List<Answer> _answer = new();
        public IReadOnlyCollection<Answer> Answers => _answer.AsReadOnly();

        //private constructor for EF Core


        protected Submission()
        {
        }

        //Factory method for creating a new submission
        public static Submission Create(int userId, Assessment assessment)
        {
            if (!assessment.IsActive)
                throw new DomainException(
                    $"Assessment '{assessment.Title}' is not currently active.");

            return new Submission
            {
                UserId = userId,
                AssessmentId = assessment.Id,
                Status = SubmissionStatus.InProgress,
                TotalScore = 0,
                StartedAt = DateTime.UtcNow
            };
        }

        //Behavior methods
        public void AddAnswer(Answer answer)
        {
            if (Status != SubmissionStatus.InProgress)
                throw new DomainException(
                    "Answers can only be added to an in-progress submission.");

            _answer.Add(answer);
        }
        public void Submit()
        {
            if (Status != SubmissionStatus.InProgress)
                throw new DomainException(
                    "Only an in-progress submission can be submitted.");

            Status = SubmissionStatus.Submitted;
            SubmittedAt = DateTime.UtcNow;
        }
        public void Evaluate()
        {
            if (Status != SubmissionStatus.Submitted)
                throw new DomainException(
                    "Only a submitted submission can be evaluated.");

            TotalScore = _answer.Sum(a => a.Score);
            Status = SubmissionStatus.Evaluated;
        }

        public bool IsCompleted =>
            Status == SubmissionStatus.Submitted ||
            Status == SubmissionStatus.Evaluated;
    }

}

