using SisMortuorio.Models.Auth;

namespace SisMortuorio.Business.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> Login(LoginRequest request);
        string GenerateJwtToken(int userId, string username, string rol);
    }
}