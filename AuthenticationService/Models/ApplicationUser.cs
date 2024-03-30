using System.ComponentModel.DataAnnotations;
using AuthenticationService.Enums;

namespace AuthenticationService.Models;

public class ApplicationUser
{
    [Key]
    public Guid UserId { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string Email { get; set; }
    public UserRoleEnum UserRole { get; set; }

    public ApplicationUser()
    {
        Username = string.Empty;
        PasswordHash = string.Empty;
        Email = string.Empty;
    }

    public ApplicationUser(Guid userId, string username, string passwordHash, string email, UserRoleEnum userRole)
    {
        UserId = userId;
        Username = username;
        PasswordHash = passwordHash;
        Email = email;
        UserRole = userRole;
    }
}