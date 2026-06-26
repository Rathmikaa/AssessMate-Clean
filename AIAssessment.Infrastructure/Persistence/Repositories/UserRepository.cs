using AIAssessment.Application.Interfaces.Repositories;
using AIAssessment.Domain.Entities;
using AIAssessment.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AIAssessment.Infrastructure.Persistence.Repositories;


/// We don't have a separate domain User table — Identity's AspNetUsers IS the user table.
/// This repository reads from Identity's DbSet and maps to a lightweight domain User
/// object for use in the Application layer (e.g. populating SubmissionSummaryDto).

public class UserRepository :  IUserRepository
{
    private readonly UserManager<IdentityUser<int>> _userManager;

    public UserRepository(UserManager<IdentityUser<int>> userManager)
    {
        _userManager = userManager;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        var identity = await _userManager.FindByIdAsync(id.ToString());
        return identity == null ? null : MapToDomain(identity);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var identity = await _userManager.FindByEmailAsync(email);
        return identity == null ? null : MapToDomain(identity);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        var identityUsers = await _userManager.Users.ToListAsync();
        return identityUsers.Select(MapToDomain);
    }

    public async Task<bool> ExistsAsync(int id)
        => await _userManager.FindByIdAsync(id.ToString()) != null;

    // Maps an Identity user → Domain User (read-only snapshot)
   
    private static User MapToDomain(IdentityUser<int> identity)
    {
        // Use User.Create to ensure the domain object is always valid.
        // The email username part is used as a fallback display name since
        // Identity doesn't have a FullName field.
        var fullName = identity.Email?.Split('@')[0] ?? "Unknown";
        return User.Create(fullName, identity.Email ?? string.Empty);
    }
}