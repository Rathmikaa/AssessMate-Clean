namespace AIAssessment.Application.Common
{
    /// <summary>
    /// Single source of truth for role name strings used across
    /// Application, Infrastructure, and API layers.
    ///
    /// Hierarchy (highest → lowest):
    ///   SuperAdmin  →  Evaluator  →  Candidate
    ///
    /// SuperAdmin  : can create / manage Evaluator accounts.
    /// Evaluator   : was formerly "Admin" — creates assessments, reviews results.
    /// Candidate   : takes assessments.
    /// </summary>
    public static class Roles
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string Evaluator = "Evaluator";
        public const string Candidate = "Candidate";
    }
}