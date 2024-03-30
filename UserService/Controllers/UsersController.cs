using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Services.UserServices;
using UserService.AsyncDataServices;
using UserService.Dtos;
using UserService.Exceptions;
using UserService.Models;
using UserService.Repositories;
using UserService.Responses;

namespace UserService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IUserService _userService;
        private readonly IMessageBusClient _messageBusClient;
        public UsersController(IMapper mapper, IUserService userService, IMessageBusClient messageBusClient)
        {
            _mapper = mapper;
            _userService = userService;
            _messageBusClient = messageBusClient;
        }

        [HttpGet]
        public async Task<IActionResult> TestReached()
        {
            Console.WriteLine("--> User service reached.");
            await Task.Delay(0);
            return Ok("User service reached!");
        }

        [HttpGet("{userId}", Name = nameof(GetUserById))]
        public async Task<IActionResult> GetUserById(Guid userId)
        {
            try
            {
                ApplicationUser user = await _userService.GetUserById(userId);
                ApplicationUserReadDto userReadDto = _mapper.Map<ApplicationUserReadDto>(user);
                return Ok(userReadDto);
            }
            catch (UserNotFoundException ex)
            {
                return NotFound(new ErrorResponse(ex.Message));
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequestModelState();
            }
            if (registerRequest.Password != registerRequest.ConfirmPassword)
            {
                return BadRequest(new ErrorResponse("Password does not match confirm password."));
            }
            ApplicationUserCreateDto userCreateDto = _mapper.Map<ApplicationUserCreateDto>(registerRequest);
            try
            {
                ApplicationUser newlyCreatedUser = await _userService.CreateUserAndSaveOutboxMessage(userCreateDto);
                ApplicationUserReadDto userReadDto = _mapper.Map<ApplicationUserReadDto>(newlyCreatedUser);

                return CreatedAtRoute(nameof(GetUserById), new { userId = userReadDto.UserId }, userReadDto);
            }
            catch (UserAlreadyExistException ex)
            {
                Console.WriteLine(ex.Message);
                return Conflict(new ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Some errors occur why trying to register new user: {ex.Message}");
                return BadRequest(new ErrorResponse($"Some errors occur why trying to register new user: {ex.Message}"));
            }
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(Guid userId)
        {
            try
            {
                await _userService.DeleteUserByIdAndSaveOutboxMessage(userId);
                return NoContent();
            }
            catch (UserNotFoundException ex)
            {
                return NotFound(new ErrorResponse(ex.Message));
            }
        }

        [HttpPut("{userId}/changepassword")]
        public async Task<IActionResult> ChangeUserPassword(Guid userId)
        {
            await Task.Delay(0);
            throw new NotImplementedException();
        }

        private IActionResult BadRequestModelState()
        {
            IEnumerable<string> errorMessages = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
            return BadRequest(new ErrorResponse(errorMessages));
        }
    }
}