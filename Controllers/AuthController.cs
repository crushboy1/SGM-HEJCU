using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SisMortuorio.Models.Auth;
using SisMortuorio.Services;

namespace SisMortuorio.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Login de usuario
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _authService.Login(request);

            if (!response.Success)
            {
                return Unauthorized(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Verificar si el token es válido (endpoint de prueba)
        /// </summary>
        [HttpGet("verify")]
        [Authorize]
        public IActionResult Verify()
        {
            var username = User.Identity?.Name;
            var role = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

            return Ok(new
            {
                message = "Token válido",
                username,
                role
            });
        }
    }
}