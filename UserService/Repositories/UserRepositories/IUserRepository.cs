using UserService.Models;

namespace Repositories.UserRepositories
{
    public interface IUserRepository
    {
        Task<ApplicationUser?> GetByEmail(string email);
        Task<ApplicationUser?> GetByUsername(string username);
        Task<ApplicationUser> Create(ApplicationUser user);
        Task<ApplicationUser?> GetById(Guid userId);
        Task Delete(ApplicationUser user);
    }
}