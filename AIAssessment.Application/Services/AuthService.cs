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
        private readonly UserManager<IdentityUser<int>> _userManager;
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

        public async Task<Result> RegisterAsync(RegisterDto dto)
        {
            var r = new Result();

            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null)
                return r.GetErrorResponse(409,
                    [$"An account with '{dto.Email}' already exists."]);

            var identityUser = new IdentityUser<int>
            {
                UserName = dto.Email,
                Email = dto.Email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(identityUser, dto.Password);
            if (!createResult.Succeeded)
                return r.GetErrorResponse(400,
                    createResult.Errors.Select(e => e.Description).ToList());

            await _userManager.AddToRoleAsync(identityUser, "Candidate");

            var (token, jwtId, expiresAt) = await _tokenService.GenerateTokenAsync(
                identityUser.Id, identityUser.Email!, "Candidate");
            await SaveTokenAsync(identityUser.Id, token, jwtId, expiresAt);

            return r.GetResponse(new AuthResultDto
            {
                UserId = identityUser.Id,
                FullName = dto.FullName,
                Email = dto.Email,
                Role = "Candidate",
                Token = token,
                ExpiresAt = expiresAt
            }, 201, [$"Account created successfully. Welcome, {dto.FullName}!"]);
        }

        public async Task<Result> LoginAsync(LoginDto dto)
        {
            var r = new Result();

            var identityUser = await _userManager.FindByEmailAsync(dto.Email);
            if (identityUser == null)
                return r.GetErrorResponse(401, ["Invalid email or password."]);

            var signInResult = await _signInManager.CheckPasswordSignInAsync(
                identityUser, dto.Password, lockoutOnFailure: false);

            if (!signInResult.Succeeded)
                return r.GetErrorResponse(401, ["Invalid email or password."]);

            await _tokenRepo.RevokeAllForUserAsync(identityUser.Id);

            var roles = await _userManager.GetRolesAsync(identityUser);
            var role = roles.FirstOrDefault() ?? "Candidate";
            var fullName = identityUser.Email!.Split('@')[0];

            var (token, jwtId, expiresAt) = await _tokenService.GenerateTokenAsync(
                identityUser.Id, identityUser.Email!, role);
            await SaveTokenAsync(identityUser.Id, token, jwtId, expiresAt);

            return r.GetResponse(new AuthResultDto
            {
                UserId = identityUser.Id,
                FullName = fullName,
                Email = identityUser.Email!,
                Role = role,
                Token = token,
                ExpiresAt = expiresAt
            }, 200, [$"Welcome back, {fullName}! You are logged in as {role}."]);
        }

        public async Task<Result> LogoutAsync(string rawToken)
        {
            var r = new Result();

            var jwtId = _tokenService.GetJwtIdFromToken(rawToken);
            if (string.IsNullOrEmpty(jwtId))
                return r.GetErrorResponse(400, ["Invalid token. Logout failed."]);

            var revoked = await _tokenRepo.RevokeAsync(jwtId);
            if (!revoked)
                return r.GetErrorResponse(400,
                    ["Token not found or already revoked. You may have already logged out."]);

            return r.GetResponse(null, 200, ["You have been logged out successfully."]);
        }

        private async Task SaveTokenAsync(
            int userId, string token, string jwtId, DateTime expiresAt)
        {
            var userToken = UserToken.Create(userId, token, jwtId, expiresAt);
            await _tokenRepo.AddAsync(userToken);
        }
    }
}