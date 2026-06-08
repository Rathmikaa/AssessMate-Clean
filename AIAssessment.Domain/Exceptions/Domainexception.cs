using System;
using System.Collections.Generic;
using System.Text;

namespace AIAssessment.Domain.Exceptions

{
    public class DomainException : Exception
    {
        public DomainException(string? message) : base(message)
        {
        }

        public DomainException(string message, string v) :base (message) { }
        
        public DomainException(string message, Exception innerException) : base(message, innerException) { }
    }
}
