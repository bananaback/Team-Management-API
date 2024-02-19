using AuthenticationService.Dtos;
using AuthenticationService.Models;

namespace AuthenticationService.Services.UserServices;

public interface IUserService
{
    Task<ApplicationUser> CreateUser(ApplicationUserCreateDto applicationUserCreateDto);
    Task<ApplicationUser?> GetUserByUsername(string username);
    Task<ApplicationUser?> GetUserByEmail(string email);
    Task DeleteUserById(Guid userId);
}