using AuthenticationService.Models;
using AuthenticationService.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.Data;

public class AuthenticationDbContext : DbContext, IUnitOfWork
{
    public AuthenticationDbContext(DbContextOptions<AuthenticationDbContext> options) : base(options)
    {

    }

    public DbSet<ApplicationUser> Users { get; set; }
}