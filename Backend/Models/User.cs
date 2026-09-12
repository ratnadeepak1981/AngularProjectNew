using System.ComponentModel.DataAnnotations;

namespace CampusServicesPortal.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public required string Email { get; set; }

    [Required]
    [MaxLength(255)]
    public required string PasswordHash { get; set; } // Will store BCrypt or Argon2 hashes

    [Required]
    [MaxLength(20)]
    public required string Role { get; set; } // Enforces "Admin" or "Student" values

    [MaxLength(150)]
    public string? FullName { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastPasswordChangedAt { get; set; } = DateTime.UtcNow;
    public bool MustChangePassword { get; set; } = false;
    public DateTime? TemporaryPasswordExpiresAt { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEndUtc { get; set; }
}
