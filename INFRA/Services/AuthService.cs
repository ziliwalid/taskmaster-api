using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DAL.CONTRACT;
using DAL.Enums;
using DAL.Models;
using INFRA.DBContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace INFRA.Services;

public class AuthService : IAuthService
{
    private readonly TaskMasterDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(TaskMasterDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<string> LoginAsync(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Email ou mot de passe incorrect");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Compte désactivé");
        }

        return GenerateJwtToken(user);
    }

    public async Task<User> RegisterAsync(string email, string firstName, string lastName, string password)
    {
        // Vérifier si l'email existe déjà
        if (await _context.Users.AnyAsync(u => u.Email == email))
        {
            throw new InvalidOperationException("Cet email est déjà utilisé");
        }

        // Créer le nouvel utilisateur
        var user = new User
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRoleEnum.User,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:SecretKey"]);
            
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (storedToken == null || !storedToken.IsActive)
        {
            throw new UnauthorizedAccessException("Token invalide");
        }

        // Marquer l'ancien token comme utilisé
        storedToken.IsUsed = true;
        
        // Générer un nouveau JWT
        var newJwtToken = GenerateJwtToken(storedToken.User);
        
        await _context.SaveChangesAsync();
        
        return newJwtToken;
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:SecretKey"]);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}