using AuthenticationService.Data;
using AuthenticationService.Models;
using AuthenticationService.Repositories.UserRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuthenticationService.Tests.Repositories.UserRepositories;

public class UserRepositoryTest
{
    private readonly DbContextOptions<AuthenticationDbContext> _options;
    private readonly AuthenticationDbContext _context;
    public UserRepositoryTest()
    {
        _options = new DbContextOptionsBuilder<AuthenticationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AuthenticationDbContext(_options);
    }

    [Fact]
    public async Task CreateUser_Success_ReturnsCreatedUser()
    {
        // Arrange
        var userRepository = new UserRepository(_context);
        var user = new ApplicationUser()
        {
            UserId = Guid.NewGuid(),
            Username = "bananaback",
            UserRole = Enums.UserRoleEnum.BASIC_USER,
            Email = "bananaback@gmail.com",
            PasswordHash = "hashedbanana",
        };

        // Act
        ApplicationUser newlyCreatedUser = await userRepository.Create(user);
        await userRepository.UnitOfWork.SaveChangesAsync();

        // Assert
        Assert.NotNull(newlyCreatedUser);

        ApplicationUser? persistedUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId.ToString() == newlyCreatedUser.UserId.ToString());

        Assert.NotNull(persistedUser);

        Assert.Equal(persistedUser.UserId.ToString(), newlyCreatedUser.UserId.ToString());

    }

    [Fact]
    public async Task CreateDuplicatedUser_Fail_ThrowsException()
    {
        // Arrange
        var userRepository = new UserRepository(_context);
        var user = new ApplicationUser()
        {
            UserId = Guid.NewGuid(),
            Username = "bananaback",
            UserRole = Enums.UserRoleEnum.BASIC_USER,
            Email = "bananaback@gmail.com",
            PasswordHash = "hashedbanana",
        };

        // Act
        await userRepository.Create(user);
        await userRepository.UnitOfWork.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await userRepository.Create(user);
            await userRepository.UnitOfWork.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task DeleteUser_Success_ReturnsNull()
    {
        // Arrange
        var userRepository = new UserRepository(_context);
        var user = new ApplicationUser()
        {
            UserId = Guid.NewGuid(),
            Username = "bananaback",
            UserRole = Enums.UserRoleEnum.BASIC_USER,
            Email = "bananaback@gmail.com",
            PasswordHash = "hashedbanana",
        };
        _context.Add(user);
        await _context.SaveChangesAsync();

        // Act
        await userRepository.DeleteUser(user);
        await userRepository.UnitOfWork.SaveChangesAsync();

        // Assert
        ApplicationUser? deletedUser = await userRepository.GetById(user.UserId);

        Assert.Null(deletedUser);
    }

    [Fact]
    public async Task DeleteUserThatNotExist_Fail_ThrowsException()
    {
        // Arrange
        var userRepository = new UserRepository(_context);
        var user = new ApplicationUser()
        {
            UserId = Guid.NewGuid(),
            Username = "bananaback",
            UserRole = Enums.UserRoleEnum.BASIC_USER,
            Email = "bananaback@gmail.com",
            PasswordHash = "hashedbanana",
        };

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
        {
            await userRepository.DeleteUser(user);
            await userRepository.UnitOfWork.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task GetUserById_Success_ReturnsUser()
    {
        // Arrange
        var userRepository = new UserRepository(_context);
        var user = new ApplicationUser()
        {
            UserId = Guid.NewGuid(),
            Username = "bananaback",
            UserRole = Enums.UserRoleEnum.BASIC_USER,
            Email = "bananaback@gmail.com",
            PasswordHash = "hashedbanana",
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        ApplicationUser? persistedUser = await userRepository.GetById(user.UserId);

        // Assert
        Assert.NotNull(persistedUser);
        Assert.Equal(persistedUser, user);
    }

    [Fact]
    public async Task GetUserById_Fail_ReturnsNull()
    {
        // Arrange
        var userRepository = new UserRepository(_context);

        // Act
        var user = await userRepository.GetById(Guid.NewGuid());

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public async Task GetUserByUsername_Success_ReturnsUser()
    {
        // Arrange
        var userRepository = new UserRepository(_context);
        var user = new ApplicationUser()
        {
            UserId = Guid.NewGuid(),
            Username = "bananaback",
            UserRole = Enums.UserRoleEnum.BASIC_USER,
            Email = "bananaback@gmail.com",
            PasswordHash = "hashedbanana",
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        ApplicationUser? persistedUser = await userRepository.GetByUsername(user.Username);

        // Assert
        Assert.NotNull(persistedUser);
        Assert.Equal(persistedUser, user);
    }

    [Fact]
    public async Task GetUserByUsername_Fail_ReturnsNull()
    {
        // Arrange
        var userRepository = new UserRepository(_context);

        // Act
        var user = await userRepository.GetByUsername("not exist");

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public async Task GetUserByEmail_Success_ReturnsUser()
    {
        // Arrange
        var userRepository = new UserRepository(_context);
        var user = new ApplicationUser()
        {
            UserId = Guid.NewGuid(),
            Username = "bananaback",
            UserRole = Enums.UserRoleEnum.BASIC_USER,
            Email = "bananaback@gmail.com",
            PasswordHash = "hashedbanana",
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        ApplicationUser? persistedUser = await userRepository.GetByEmail(user.Email);

        // Assert
        Assert.NotNull(persistedUser);
        Assert.Equal(persistedUser, user);
    }

    [Fact]
    public async Task GetUserByEmail_Fail_ReturnsNull()
    {
        // Arrange
        var userRepository = new UserRepository(_context);

        // Act
        var user = await userRepository.GetByEmail("notexist@test.com");

        // Assert
        Assert.Null(user);
    }
}