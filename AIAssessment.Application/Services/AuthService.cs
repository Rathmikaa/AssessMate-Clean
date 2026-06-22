using AIAssessment.Application.Common;
using AIAssessment.Application.DTOs.Auth;
using AIAssessment.Application.Interfaces.Repositories;
using AIAssessment.Application.Interfaces.Services;
using AIAssessment.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace AIAssessment.Application.Services
{
    public class AuthService
    {
        private readonly UserManager<IdentityUser<int>> _userManager;
        private readonly SignInManager<IdentityUser<int>> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IUserTokenRepository _tokenRepo;
        private readonly IRefreshTokenRepository _refreshTokenRepo;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<IdentityUser<int>> userManager,
            SignInManager<IdentityUser<int>> signInManager,
            ITokenService tokenService,
            IUserTokenRepository tokenRepo,
            IRefreshTokenRepository refreshTokenRepo,
            IEmailService emailService,
            IConfiguration config,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _tokenRepo = tokenRepo;
            _refreshTokenRepo = refreshTokenRepo;
            _emailService = emailService;
            _config = config;
            _logger = logger;
        }

        public async Task<Result> RegisterAsync(RegisterDto dto)
        {
            var r = new Result();

            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null)
                return r.GetErrorResponse(409, [$"An account with '{dto.Email}' already exists."]);

            var identityUser = new IdentityUser<int>
            {
                UserName = dto.FullName,
                Email = dto.Email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(identityUser, dto.Password);
            if (!createResult.Succeeded)
                return r.GetErrorResponse(400, createResult.Errors.Select(e => e.Description).ToList());

            await _userManager.AddToRoleAsync(identityUser, "Candidate");

            var (token, jwtId, expiresAt) = await _tokenService.GenerateTokenAsync(
                identityUser.Id, identityUser.Email!, "Candidate");
            await SaveTokenAsync(identityUser.Id, token, jwtId, expiresAt);
            var refreshToken = await IssueRefreshTokenAsync(identityUser.Id);

            return r.GetResponse(new AuthResultDto
            {
                UserId = identityUser.Id,
                FullName = identityUser.UserName!,
                Email = dto.Email,
                Role = "Candidate",
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt
            }, 201, [$"Account created successfully. Welcome, {identityUser.UserName}!"]);
        }

        public async Task<Result> LoginAsync(LoginDto dto)
        {
            var r = new Result();

            var identityUser = await _userManager.FindByEmailAsync(dto.Email);
            if (identityUser == null)
                return r.GetErrorResponse(401, ["Invalid email or password."]);

            var signInResult = await _signInManager.CheckPasswordSignInAsync(
                identityUser, dto.Password, lockoutOnFailure: true);

            if (signInResult.IsLockedOut)
                return r.GetErrorResponse(403,
                    ["This account is locked or has been deactivated. Please contact an administrator, or try again later."]);

            if (!signInResult.Succeeded)
                return r.GetErrorResponse(401, ["Invalid email or password."]);

            // One active session per user — wipe any existing access + refresh tokens.
            await _tokenRepo.RevokeAllForUserAsync(identityUser.Id);
            await _refreshTokenRepo.RevokeAllForUserAsync(identityUser.Id);

            var roles = await _userManager.GetRolesAsync(identityUser);
            var role = roles.FirstOrDefault() ?? "Candidate";

            // FIX: this used to re-derive a fake name from the email prefix instead of
            // reading back what was actually registered.
            var fullName = identityUser.UserName ?? identityUser.Email!.Split('@')[0];

            var (token, jwtId, expiresAt) = await _tokenService.GenerateTokenAsync(
                identityUser.Id, identityUser.Email!, role);
            await SaveTokenAsync(identityUser.Id, token, jwtId, expiresAt);
            var refreshToken = await IssueRefreshTokenAsync(identityUser.Id);

            return r.GetResponse(new AuthResultDto
            {
                UserId = identityUser.Id,
                FullName = fullName,
                Email = identityUser.Email!,
                Role = role,
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt
            }, 200, [$"Welcome back, {fullName}! You are logged in as {role}."]);
        }

        public async Task<Result> RefreshAsync(RefreshTokenDto dto)
        {
            var r = new Result();
            var stored = await _refreshTokenRepo.GetByTokenAsync(dto.RefreshToken);
            if (stored == null || !stored.IsActive)
                return r.GetErrorResponse(401, ["Refresh token is invalid or has expired. Please log in again."]);

            var identityUser = await _userManager.FindByIdAsync(stored.UserId.ToString());
            if (identityUser == null)
                return r.GetErrorResponse(401, ["User no longer exists."]);

            // Rotate: old refresh token is single-use.
            await _refreshTokenRepo.RevokeAsync(dto.RefreshToken);
            await _tokenRepo.RevokeAllForUserAsync(identityUser.Id);

            var roles = await _userManager.GetRolesAsync(identityUser);
            var role = roles.FirstOrDefault() ?? "Candidate";
            var fullName = identityUser.UserName ?? identityUser.Email!.Split('@')[0];

            var (token, jwtId, expiresAt) = await _tokenService.GenerateTokenAsync(identityUser.Id, identityUser.Email!, role);
            await SaveTokenAsync(identityUser.Id, token, jwtId, expiresAt);
            var newRefreshToken = await IssueRefreshTokenAsync(identityUser.Id);

            return r.GetResponse(new AuthResultDto
            {
                UserId = identityUser.Id,
                FullName = fullName,
                Email = identityUser.Email!,
                Role = role,
                Token = token,
                RefreshToken = newRefreshToken,
                ExpiresAt = expiresAt
            }, 200, ["Token refreshed successfully."]);
        }

        public async Task<Result> LogoutAsync(string rawToken, int userId)
        {
            var r = new Result();
            var jwtId = _tokenService.GetJwtIdFromToken(rawToken);
            if (string.IsNullOrEmpty(jwtId))
                return r.GetErrorResponse(400, ["Invalid token. Logout failed."]);

            var revoked = await _tokenRepo.RevokeAsync(jwtId);
            await _refreshTokenRepo.RevokeAllForUserAsync(userId);

            if (!revoked)
                return r.GetErrorResponse(400, ["Token not found or already revoked. You may have already logged out."]);

            return r.GetResponse(null, 200, ["You have been logged out successfully."]);
        }

        public async Task<Result> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var r = new Result();
            const string genericMessage = "If an account with that email exists, a password reset link has been sent.";

            var identityUser = await _userManager.FindByEmailAsync(dto.Email);
            if (identityUser != null)
            {
                try
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(identityUser);
                    var baseUrl = _config["Frontend:BaseUrl"] ?? "http://localhost:4200";
                    var link = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(identityUser.Email!)}&token={Uri.EscapeDataString(token)}";
                    await _emailService.SendPasswordResetEmailAsync(identityUser.Email!, identityUser.UserName ?? "there", link);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send password reset email to {Email}", dto.Email);
                }
            }

            // Same response either way — prevents attackers discovering which emails are registered.
            return r.GetResponse(null, 200, [genericMessage]);
        }

        public async Task<Result> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var r = new Result();
            var identityUser = await _userManager.FindByEmailAsync(dto.Email);
            if (identityUser == null)
                return r.GetErrorResponse(400, ["Invalid or expired reset link."]);

            var result = await _userManager.ResetPasswordAsync(identityUser, dto.Token, dto.NewPassword);
            if (!result.Succeeded)
                return r.GetErrorResponse(400, result.Errors.Select(e => e.Description).ToList());

            await _tokenRepo.RevokeAllForUserAsync(identityUser.Id);
            await _refreshTokenRepo.RevokeAllForUserAsync(identityUser.Id);

            return r.GetResponse(null, 200, ["Your password has been reset. Please log in with your new password."]);
        }

        private async Task SaveTokenAsync(int userId, string token, string jwtId, DateTime expiresAt)
        {
            var userToken = UserToken.Create(userId, token, jwtId, expiresAt);
            await _tokenRepo.AddAsync(userToken);
        }

        private async Task<string> IssueRefreshTokenAsync(int userId)
        {
            var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var refreshToken = RefreshToken.Create(userId, raw);
            await _refreshTokenRepo.AddAsync(refreshToken);
            return raw;
        }
    }
}