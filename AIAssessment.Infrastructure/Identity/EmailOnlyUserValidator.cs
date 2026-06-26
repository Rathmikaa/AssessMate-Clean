using Microsoft.AspNetCore.Identity;

namespace AIAssessment.Infrastructure.Identity
{
    /// <summary>
    /// Replaces Identity's default UserValidator. The default one rejects:
    ///   1. Duplicate UserName — a problem now that UserName stores the person's
    ///      display name, since two different candidates can share a name.
    ///   2. Spaces in UserName — most full names ("Jane Doe") have one.
    /// Email uniqueness is still enforced below, and login is always by email anyway.
    /// </summary>
    public class EmailOnlyUserValidator : IUserValidator<IdentityUser<int>>
    {
        public async Task<IdentityResult> ValidateAsync(
            UserManager<IdentityUser<int>> manager, IdentityUser<int> user)
        {
            var errors = new List<IdentityError>();

            if (string.IsNullOrWhiteSpace(user.UserName))
                errors.Add(new IdentityError { Code = "InvalidUserName", Description = "Name is required." });

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                errors.Add(new IdentityError { Code = "InvalidEmail", Description = "Email is required." });
            }
            else
            {
                var owner = await manager.FindByEmailAsync(user.Email);
                if (owner != null && !owner.Id.Equals(user.Id))
                    errors.Add(new IdentityError { Code = "DuplicateEmail", Description = $"Email '{user.Email}' is already taken." });
            }

            return errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed(errors.ToArray());
        }
    }
}