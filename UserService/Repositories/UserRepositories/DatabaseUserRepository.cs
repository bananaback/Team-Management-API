using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Exceptions;
using UserService.Models;
using UserService.Repositories;

namespace Repositories.UserRepositories
{
    public class DatabaseUserRepository : IUserRepository
    {
        private readonly UserDbContext _context;
        public DatabaseUserRepository(UserDbContext userDbContext)
        {
            _context = userDbContext;
        }

        public IUnitOfWork UnitOfWork => _context;
        public async Task<ApplicationUser> Create(ApplicationUser user)
        {
            await _context.Users.AddAsync(user);
            return user;
        }

        public async Task Delete(ApplicationUser user)
        {
            _context.Users.Remove(user);
            await Task.CompletedTask;
        }

        public async Task<ApplicationUser?> GetByEmail(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<ApplicationUser?> GetById(Guid userId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<ApplicationUser?> GetByUsername(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }
    }
}