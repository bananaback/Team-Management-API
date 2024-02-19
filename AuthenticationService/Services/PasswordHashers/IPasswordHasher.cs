namespace AuthenticationService.Services.PasswordHashers;

public interface IPasswordHasher
{
    public string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}