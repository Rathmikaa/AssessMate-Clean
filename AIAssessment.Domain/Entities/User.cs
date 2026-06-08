using System;
using System.Collections.Generic;
using System.Text;
using AIAssessment.Domain.Exceptions;
namespace AIAssessment.Domain.Entities
{
   
    public class User
    {
        public int Id { get; private set; }
        public string FullName { get; private set; }
        public String Email { get; private set; }

        //Summary <soft delete flag>
        public bool IsActive { get; private set; } 
        public DateTime DateTime { get; private set; }

        // Navigation 
        protected readonly List<Submission> _submissions = new();
        public IReadOnlyCollection<Submission> Submissions => _submissions.AsReadOnly();

        //private constructor  
        protected User() 
        {
            FullName = string.Empty;
            Email = string.Empty;
        }

        public static  User Create (string fullName, string email)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(fullName))
                throw new DomainException("Full name is required.", nameof(fullName));
            if (string.IsNullOrWhiteSpace(email))
                throw new DomainException("Email is required.", nameof(email));
            return new User
            {
                FullName = fullName,
                Email = email,
                IsActive = true,
                DateTime = DateTime.UtcNow
            };
        }

        //Behavior methods 

        public void Deactivate()
        {
            IsActive = false;
        }
        public void Reactivate()
        {
            IsActive = true;
        }

        public void UpdateFullName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new DomainException("Full name cannot be Empty", nameof(newName));
            FullName = newName.Trim();
        }

    }
}
