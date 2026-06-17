using AIAssessment.Application.Common;
using AIAssessment.Application.DTOs.Auth;
using AIAssessment.Application.Interfaces.Repositories;
using AIAssessment.Application.Interfaces.Services;
using AIAssessment.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AIAssessment.Application.Services
{
    public class AuthService
    {
        private readonly  UserManager<IdentityUser<int>> _userManager;
        private readonly SignInManager<IdentityUser<int>> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IUserTokenRepository _tokenRepo;

        public AuthService(
            UserManager<IdentityUser<int>> userManager,
            SignInManager<IdentityUser<int>> signInManager,
            ITokenService tokenService,
            IUserTokenRepository tokenRepo)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _tokenRepo = tokenRepo;
        }

        // Register 
        public async Task<Result<AuthResultDto>> RegisterAsync(RegisterDto dto)
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null)
                return Result<AuthResultDto>.Failure(
                    "An account with this email already exists.");

            var identityUser = new IdentityUser<int>
            {
                UserName = dto.Email,
                Email = dto.Email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(identityUser, dto.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                return Result<AuthResultDto>.Failure(errors);
            }

            await _userManager.AddToRoleAsync(identityUser, "Candidate");

            // Pass identityUser.Id directly — this is the real DB-assigned ID
            // NOT a domain User object (whose Id would always be 0)
            var (token, jwtId, expiresAt) = await _tokenService.GenerateTokenAsync(
                identityUser.Id, identityUser.Email!, "Candidate");

            await SaveTokenAsync(identityUser.Id, token, jwtId, expiresAt);

            return Result<AuthResultDto>.Success(new AuthResultDto
            {
                UserId = identityUser.Id,
                FullName = dto.FullName,
                Email = dto.Email,
                Role = "Candidate",
                Token = token,
                ExpiresAt = expiresAt
            });
        }

        // Login
        public async Task<Result<AuthResultDto>> LoginAsync(LoginDto dto)
        {
            var identityUser = await _userManager.FindByEmailAsync(dto.Email);
            if (identityUser == null)
                return Result<AuthResultDto>.Failure("Invalid email or password.");

            var signInResult = await _signInManager.CheckPasswordSignInAsync(
                identityUser, dto.Password, lockoutOnFailure: false);

            if (!signInResult.Succeeded)
                return Result<AuthResultDto>.Failure("Invalid email or password.");

            // ONE SESSION PER USER
            await _tokenRepo.RevokeAllForUserAsync(identityUser.Id);

            var roles = await _userManager.GetRolesAsync(identityUser);
            var role = roles.FirstOrDefault() ?? "Candidate";
            var fullName = identityUser.Email!.Split('@')[0];

            // Pass real Identity ID — not a domain User
            var (token, jwtId, expiresAt) = await _tokenService.GenerateTokenAsync(
                identityUser.Id, identityUser.Email!, role);

            await SaveTokenAsync(identityUser.Id, token, jwtId, expiresAt);

            return Result<AuthResultDto>.Success(new AuthResultDto
            {
                UserId = identityUser.Id,
                FullName = fullName,
                Email = identityUser.Email!,
                Role = role,
                Token = token,
                ExpiresAt = expiresAt
            });
        }

        // Logout
        public async Task<Result> LogoutAsync(string rawToken)
        {
            var jwtId = _tokenService.GetJwtIdFromToken(rawToken);
            if (string.IsNullOrEmpty(jwtId))
                return Result.Failure("Invalid token.");

            var revoked = await _tokenRepo.RevokeAsync(jwtId);
            if (!revoked)
                return Result.Failure("Token not found or already revoked.");

            return Result.Success();
        }

        //  Private helper
        private async Task SaveTokenAsync(
            int userId, string token, string jwtId, DateTime expiresAt)
        {
            var userToken = UserToken.Create(userId, token, jwtId, expiresAt);
            await _tokenRepo.AddAsync(userToken);
        }
    }
}