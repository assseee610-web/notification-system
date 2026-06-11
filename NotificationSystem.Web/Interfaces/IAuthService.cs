using NotificationSystem.Shared.DTOs;

namespace NotificationSystem.Web.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
}