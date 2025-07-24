using DAL.Models;

namespace DAL.CONTRACT;

public interface IAuthService
{
    Task<string> LoginAsync(string email, string password);
    Task<User> RegisterAsync(string email, string password, string firstName, string lastName);
    Task<bool> ValidateTokenAsync(string token);
    Task<string> RefreshTokenAsync(string refreshToken);
}