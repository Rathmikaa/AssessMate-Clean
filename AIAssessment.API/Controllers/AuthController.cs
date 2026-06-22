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

        /// <summary>
        /// Registers a new user with the provided details.
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
            => ToResponse(await _authService.RegisterAsync(dto));
        /// <summary>
        /// login a user with the provided credentials and returns an authentication token if successful.
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
            => ToResponse(await _authService.LoginAsync(dto));
        /// <summary>
        /// logouts the user by invalidating the provided authentication token, effectively ending the user's session.
        /// </summary>
        /// <returns></returns>

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