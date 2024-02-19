using AuthenticationService.Data;
using AuthenticationService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.Repositories.UserRepositories;

public class UserRepository : IUserRepository
{
    private readonly AuthenticationDbContext _dbContext;
    public UserRepository(AuthenticationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IUnitOfWork UnitOfWork => _dbContext;

    public async Task<ApplicationUser> Create(ApplicationUser user)
    {
        await _dbContext.Users.AddAsync(user);
        return user;
    }

    public async Task DeleteUser(ApplicationUser user)
    {
        _dbContext.Users.Remove(user);
        await Task.CompletedTask;
    }

    public async Task<ApplicationUser?> GetByEmail(string email)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<ApplicationUser?> GetByExternalId(Guid externalId)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.ExternalUserId == externalId);
    }

    public async Task<ApplicationUser?> GetByUsername(string username)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
    }
}