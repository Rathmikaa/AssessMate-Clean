using AIAssessment.Application.Interfaces.Services;
using AIAssessment.Domain.Entities;
using AIAssessment.Domain.Enums;

namespace AIAssessment.Infrastructure.Services;

/// <summary>
/// Simple keyword-based scoring for Descriptive answers.
///
/// Algorithm:
///   1. Tokenise the ModelAnswer into individual words (keywords).
///   2. Count how many keywords appear in the candidate's answer (case-insensitive).
///   3. Score = (matchedKeywords / totalKeywords) × MaxMarks, rounded down.
///
/// WHY IS THIS HERE AND NOT IN DOMAIN OR APPLICATION?
///   - Domain: pure business rules, no algorithms.
///   - Application: orchestration, no implementation details.
///   - Infrastructure: THIS IS THE IMPLEMENTATION. A future AiScoringService
///     would also live here and call an external API. You'd register whichever
///     one you want in DependencyInjection.cs without touching anything else.
///
/// SWAPPING TO AI SCORING:
///   Create AiScoringService : IScoringService in this folder.
///   In DependencyInjection.cs, change:
///     services.AddScoped<IScoringService, KeywordScoringService>();
///   to:
///     services.AddScoped<IScoringService, AiScoringService>();
///   Nothing else needs to change anywhere.

public class KeywordScoringService : IScoringService
{
    // Words to ignore when matching — they appear everywhere and add no signal
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "is", "are", "was", "were", "be", "been",
        "being", "have", "has", "had", "do", "does", "did", "will",
        "would", "could", "should", "may", "might", "shall", "can",
        "to", "of", "in", "on", "at", "by", "for", "with", "about",
        "and", "or", "but", "not", "it", "this", "that", "which"
    };

    public Task<int> ScoreAnswerAsync(Question question, Answer answer)
    {
        int score = question.QuestionType switch
        {
            QuestionType.MCQ => ScoreMcq(question, answer),
            QuestionType.Descriptive => ScoreDescriptive(question, answer),
            _ => 0
        };

        return Task.FromResult(score);
    }

    // -------------------------------------------------------------------------
    // MCQ: full marks if the selected option is correct, else zero
    // -------------------------------------------------------------------------
    private static int ScoreMcq(Question question, Answer answer)
    {
        if (answer.SelectedOptionId == null)
            return 0;

        var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);

        return correctOption != null && answer.SelectedOptionId == correctOption.Id
            ? question.MaxMarks
            : 0;
    }

    // -------------------------------------------------------------------------
    // Descriptive: keyword match ratio × MaxMarks
    // -------------------------------------------------------------------------
    private static int ScoreDescriptive(Question question, Answer answer)
    {
        if (string.IsNullOrWhiteSpace(question.ModelAnswer))
            return 0;

        if (string.IsNullOrWhiteSpace(answer.AnswerText))
            return 0;

        // Tokenise model answer — split on non-word characters, remove stop words
        var keywords = Tokenise(question.ModelAnswer)
            .Where(w => !StopWords.Contains(w))
            .Distinct()
            .ToList();

        if (keywords.Count == 0)
            return 0;

        var candidateWords = Tokenise(answer.AnswerText).ToHashSet(StringComparer.OrdinalIgnoreCase);

        int matched = keywords.Count(kw => candidateWords.Contains(kw));

        // Integer division intentional — we floor the score
        return (int)((double)matched / keywords.Count * question.MaxMarks);
    }

    private static IEnumerable<string> Tokenise(string text)
        => text
            .ToLowerInvariant()
            .Split(new[] { ' ', ',', '.', ';', ':', '!', '?', '\n', '\r', '\t', '-', '(', ')' },
                   StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 1); // single characters are noise
}