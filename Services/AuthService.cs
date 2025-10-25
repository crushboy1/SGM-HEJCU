using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SisMortuorio.Data;
using SisMortuorio.Data.Entities;
using SisMortuorio.Models.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SisMortuorio.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly SignInManager<Usuario> _signInManager;
        private readonly IConfiguration _configuration;

        public AuthService(
            ApplicationDbContext context,
            UserManager<Usuario> userManager,
            SignInManager<Usuario> signInManager,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        public async Task<LoginResponse> Login(LoginRequest request)
        {
            try
            {
                // Buscar usuario por username
                var usuario = await _context.Users
                    .Include(u => u.Rol)
                    .FirstOrDefaultAsync(u => u.UserName == request.Username);

                if (usuario == null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        ErrorMessage = "Usuario no encontrado"
                    };
                }

                // Verificar si el usuario está activo
                if (!usuario.Activo)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        ErrorMessage = "Usuario inactivo"
                    };
                }

                // Verificar contraseña usando Identity
                var result = await _signInManager.CheckPasswordSignInAsync(
                    usuario,
                    request.Password,
                    lockoutOnFailure: false);

                if (!result.Succeeded)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        ErrorMessage = "Contraseña incorrecta"
                    };
                }

                // Actualizar último acceso
                usuario.UltimoAcceso = DateTime.Now;
                await _context.SaveChangesAsync();

                // Generar token
                var token = GenerateJwtToken(usuario.Id, usuario.UserName!, usuario.Rol.Name!);
                var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "480");

                return new LoginResponse
                {
                    Success = true,
                    Token = token,
                    Username = usuario.UserName!,
                    NombreCompleto = usuario.NombreCompleto,
                    Rol = usuario.Rol.Name!,
                    Expiration = DateTime.UtcNow.AddMinutes(expirationMinutes)
                };
            }
            catch (Exception ex)
            {
                return new LoginResponse
                {
                    Success = false,
                    ErrorMessage = $"Error interno: {ex.Message}"
                };
            }
        }

        public string GenerateJwtToken(int userId, string username, string rol)
        {
            var secretKey = _configuration["Jwt:SecretKey"]
                ?? throw new InvalidOperationException("JWT SecretKey no configurada");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, rol),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "480");

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}