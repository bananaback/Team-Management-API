using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Exceptions;
using UserService.Models;

namespace Repositories.UserRepositories
{
    public class DatabaseUserRepository : IUserRepository
    {
        private readonly UserDbContext _context;
        public DatabaseUserRepository(UserDbContext userDbContext)
        {
            _context = userDbContext;
        }
        public async Task<ApplicationUser> Create(ApplicationUser user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task Delete(ApplicationUser user)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
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