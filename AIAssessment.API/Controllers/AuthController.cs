using AIAssessment.Application.Common;
using AIAssessment.Application.DTOs.Auth;
using AIAssessment.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIAssessment.API.Controllers
{
    [Route("api/auth")]
    public class AuthController : BaseController
    {
        private readonly AuthService _authService;
        public AuthController(AuthService authService) => _authService = authService;

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
            => ToResponse(await _authService.RegisterAsync(dto));

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
            => ToResponse(await _authService.LoginAsync(dto));

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenDto dto)
            => ToResponse(await _authService.RefreshAsync(dto));

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
            => ToResponse(await _authService.ForgotPasswordAsync(dto));

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
            => ToResponse(await _authService.ResetPasswordAsync(dto));

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var authHeader = Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                var r = new Result();
                return ToResponse(r.GetErrorResponse(400, ["No token provided."]));
            }

            var rawToken = authHeader["Bearer ".Length..].Trim();
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = int.TryParse(userIdClaim, out var id) ? id : 0;

            return ToResponse(await _authService.LogoutAsync(rawToken, userId));
        }
    }
}