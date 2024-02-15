using UserService.Dtos;
using UserService.Models;

namespace Services.UserServices
{
    public interface IUserService
    {
        Task<ApplicationUser> GetUserByEmail(string email);
        Task<ApplicationUser> GetUserByUsername(string username);
        Task<ApplicationUser> CreateUserAndSaveOutboxMessage(ApplicationUserCreateDto user);
        Task<ApplicationUser> GetUserById(Guid userId);
        Task DeleteUserById(Guid userId);
    }
}
