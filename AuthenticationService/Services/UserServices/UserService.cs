using AuthenticationService.Dtos;
using AuthenticationService.Exceptions;
using AuthenticationService.Models;
using AuthenticationService.Repositories.UserRepositories;
using AuthenticationService.Services.CacheServices;
using AutoMapper;

namespace AuthenticationService.Services.UserServices;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenCache _redisTokenCache;
    private readonly IMapper _mapper;
    public UserService(IUserRepository userRepository, IMapper mapper, ITokenCache redisTokenCache)
    {
        _userRepository = userRepository;
        _redisTokenCache = redisTokenCache;
        _mapper = mapper;
    }

    public async Task<ApplicationUser> CreateUser(ApplicationUserCreateDto applicationUserCreateDto)
    {
        ApplicationUser? existingUserWithUsername = await _userRepository.GetByUsername(applicationUserCreateDto.Username);
        ApplicationUser? existingUserWithEmail = await _userRepository.GetByEmail(applicationUserCreateDto.Email);
        if (existingUserWithUsername is not null)
        {
            throw new UserAlreadyExistException($"User with username {applicationUserCreateDto.Username} already exist.");
        }

        if (existingUserWithEmail is not null)
        {
            throw new UserAlreadyExistException($"User with email {applicationUserCreateDto.Email} already exist.");
        }
        ApplicationUser user = _mapper.Map<ApplicationUser>(applicationUserCreateDto);
        user.UserId = Guid.NewGuid();
        ApplicationUser newlyCreatedUser = await _userRepository.Create(user);
        await _userRepository.UnitOfWork.SaveChangesAsync();

        return newlyCreatedUser;
    }

    public async Task DeleteUserByIdAndRevokeAllToken(Guid userId)
    {
        ApplicationUser? user = await _userRepository.GetById(userId);
        if (user == null)
        {
            throw new UserNotFoundException($"User with id {userId} not found.");
        }
        await _userRepository.DeleteUser(user);
        await _redisTokenCache.RevokeAllRefreshTokensOfUser(userId.ToString());
        await _userRepository.UnitOfWork.SaveChangesAsync();
    }

    public Task<ApplicationUser?> GetUserByEmail(string email)
    {
        throw new NotImplementedException();
    }

    public Task<ApplicationUser?> GetUserByUsername(string username)
    {
        throw new NotImplementedException();
    }
}