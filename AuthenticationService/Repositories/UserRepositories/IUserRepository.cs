using AuthenticationService.Models;

namespace AuthenticationService.Repositories.UserRepositories;

public interface IUserRepository
{
    IUnitOfWork UnitOfWork { get; }
    Task<ApplicationUser> Create(ApplicationUser user);
    Task<ApplicationUser?> GetById(Guid id);
    Task<ApplicationUser?> GetByUsername(string username);
    Task<ApplicationUser?> GetByEmail(string email);
    Task DeleteUser(ApplicationUser user);
}