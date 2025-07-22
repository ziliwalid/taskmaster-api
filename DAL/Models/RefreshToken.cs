using System.ComponentModel.DataAnnotations;

namespace DAL.Models;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string Token { get; set; } = string.Empty;
    
    public DateTime ExpiryDate { get; set; }
    public bool IsUsed { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public string? ReplacedByToken { get; set; }
    public string? ReasonRevoked { get; set; }
    
    // Foreign key
    public Guid UserId { get; set; }
    
    // Navigation property
    public User User { get; set; } = null!;
    
    public bool IsActive => !IsRevoked && !IsUsed && DateTime.UtcNow <= ExpiryDate;
}
