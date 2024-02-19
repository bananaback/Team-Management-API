using AuthenticationService.Enums;
using AuthenticationService.Models;

namespace AuthenticationService.Dtos;

public class ApplicationUserCreateDto
{
    public Guid UserId { get; set; }
    public Guid ExternalUserId { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string Email { get; set; }
    public UserRoleEnum UserRole { get; set; }

    public ApplicationUserCreateDto()
    {
        Username = string.Empty;
        PasswordHash = string.Empty;
        Email = string.Empty;
    }

    public ApplicationUserCreateDto(Guid externalUserId, string username, string passwordHash, string email, UserRoleEnum userRole)
    {
        ExternalUserId = externalUserId;
        Username = username;
        PasswordHash = passwordHash;
        Email = email;
        UserRole = userRole;
    }
}