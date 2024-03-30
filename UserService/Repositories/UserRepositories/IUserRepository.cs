using UserService.Models;
using UserService.Repositories;

namespace Repositories.UserRepositories
{
    public interface IUserRepository
    {
        IUnitOfWork UnitOfWork { get; }
        Task<ApplicationUser?> GetByEmail(string email);
        Task<ApplicationUser?> GetByUsername(string username);
        Task<ApplicationUser> Create(ApplicationUser user);
        Task<ApplicationUser?> GetById(Guid userId);
        Task Delete(ApplicationUser user);
    }
}