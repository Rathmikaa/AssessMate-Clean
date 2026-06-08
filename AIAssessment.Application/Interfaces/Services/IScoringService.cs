using AIAssessment.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIAssessment.Application.Interfaces.Services
{
    public interface IScoringService
    {
        Task<int> ScoreAnswerAsync(Question question, Answer answer);
    }
}
