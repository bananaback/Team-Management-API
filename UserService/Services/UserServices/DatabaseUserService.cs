using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Exceptions;
using UserService.Models;
using Repositories.UserRepositories;
using UserService.Dtos;
using AutoMapper;
using UserService.PasswordHashers;
using Microsoft.AspNetCore.Http.HttpResults;
using UserService.Repositories.OutboxRepositories;
using System.Text.Json;
using UserService.Enums;

namespace Services.UserServices
{
    public class DatabaseUserService : IUserService
    {
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUserRepository _userRepository;
        private readonly IOutboxRepository _outboxRepository;
        private readonly IMapper _mapper;

        public DatabaseUserService(IMapper mapper, IUserRepository userRepository, IPasswordHasher passwordHasher, IOutboxRepository outboxRepository)
        {
            _mapper = mapper;
            _userRepository = userRepository;
            _outboxRepository = outboxRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<ApplicationUser> GetUserByEmail(string email)
        {
            ApplicationUser? user = await _userRepository.GetByEmail(email);
            return user ?? throw new UserNotFoundException($"User with email {email} not found.");
        }

        public async Task<ApplicationUser> GetUserByUsername(string username)
        {
            ApplicationUser? user = await _userRepository.GetByUsername(username);
            return user ?? throw new UserNotFoundException($"User with username {username} not found.");
        }

        public async Task<ApplicationUser> CreateUserAndSaveOutboxMessage(ApplicationUserCreateDto userCreateDto)
        {
            ApplicationUser? existingUserWithUsername = await _userRepository.GetByUsername(userCreateDto.Username);
            ApplicationUser? existingUserWithEmail = await _userRepository.GetByEmail(userCreateDto.Email);
            if (existingUserWithUsername is not null)
            {
                throw new UserAlreadyExistException($"User with username {userCreateDto.Username} already exist.");
            }

            if (existingUserWithEmail is not null)
            {
                throw new UserAlreadyExistException($"User with email {userCreateDto.Email} already exist.");
            }
            ApplicationUser user = _mapper.Map<ApplicationUser>(userCreateDto);
            user.UserId = Guid.NewGuid();
            user.PasswordHash = _passwordHasher.HashPassword(userCreateDto.Password);
            user.UserRole = UserRoleEnum.BASIC_USER;
            ApplicationUser newlyCreatedUser = await _userRepository.Create(user);

            OutboxMessage userCreatedOutboxMessage = new OutboxMessage(
                JsonSerializer.Serialize(newlyCreatedUser),
                "User_Created",
                false,
                DateTime.Now);

            await _outboxRepository.Create(userCreatedOutboxMessage);

            await _userRepository.UnitOfWork.SaveChangesAsync();
            return newlyCreatedUser;
        }

        public async Task<ApplicationUser> GetUserById(Guid userId)
        {
            ApplicationUser? user = await _userRepository.GetById(userId);
            return user ?? throw new UserNotFoundException($"User with id {userId} not found.");
        }

        public async Task DeleteUserByIdAndSaveOutboxMessage(Guid userId)
        {
            ApplicationUser? user = await _userRepository.GetById(userId);
            if (user is not null)
            {
                await _userRepository.Delete(user);
                OutboxMessage userDeletedOutboxMessage = new OutboxMessage(
                    userId.ToString(),
                    "User_Deleted",
                    false,
                    DateTime.Now
                );
                await _outboxRepository.Create(userDeletedOutboxMessage);
                await _userRepository.UnitOfWork.SaveChangesAsync();
            }
            else
            {
                throw new UserNotFoundException($"User with id {userId} not found.");
            }
        }
    }
}
