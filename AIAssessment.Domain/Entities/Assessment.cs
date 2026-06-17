using System;
using System.Collections.Generic;
using System.Text;
using AIAssessment.Domain.Enums;
using AIAssessment.Domain.Exceptions;

namespace AIAssessment.Domain.Entities
{
    public class Assessment
    { 
        public int Id { get; private set; }
        public string Title { get; private set; }
        public string? Description { get; private set; }

        public int DurationMinutes { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; } 
        

        //Navigation
        protected readonly List<Question> _questions = new();
        public IReadOnlyCollection<Question> Questions => _questions.AsReadOnly();

        protected readonly List<Submission> _submissions = new();
        public IReadOnlyCollection<Submission> Submissions => _submissions.AsReadOnly();

        //private constructor for EF Core

        protected Assessment()
        {
            Title = string.Empty;
        }

        // Factory method for creating a new assessment

        public static Assessment Create(string title, string? description, int durationMinutes)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Title is required.", nameof(title));
            if (durationMinutes <= 0)
                throw new DomainException("Duration must be greater than zero.", nameof(durationMinutes));
            return new Assessment
            {
                Title = title,
                Description = description,
                DurationMinutes = durationMinutes,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        //Behaviour methods

        public void Update(string title, string? description, int durationMinutes)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Assessment title is required.");
            if (durationMinutes <= 0)
                throw new DomainException("Duration must be grater than zero.");
            Title = title.Trim();
            Description = description?.Trim();
            DurationMinutes = durationMinutes;

        }

        public void Activate() => IsActive = true;
        public void Deactivate() => IsActive = false;


        public void AddQuestions(Question question)
        {
            if (!IsActive)
                throw new DomainException("Cannot add Questions to an inactive assessment. ");
            _questions.Add(question);
        }

        public void RemoveQuestion(Question question)
        {
            _questions.Remove(question);
        }

    }
}
