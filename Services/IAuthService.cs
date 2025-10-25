using SisMortuorio.Models.Auth;

namespace SisMortuorio.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> Login(LoginRequest request);
        string GenerateJwtToken(int userId, string username, string rol);
    }
}