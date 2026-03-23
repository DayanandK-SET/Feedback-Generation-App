using Feedback_Generation_App.Exceptions;
using Feedback_Generation_App.Interfaces;
using Feedback_Generation_App.Models;
using Feedback_Generation_App.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Feedback_Generation_App.Services
{
    public class UserService : IUserService
    {
        private readonly IRepository<int, User> _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IPasswordService _passwordService;

        public UserService(
            IRepository<int, User> userRepository,
            IPasswordService passwordService,
            ITokenService tokenService)
        {
            _userRepository = userRepository;
            _passwordService = passwordService;
            _tokenService = tokenService;
        }

        public async Task<CheckUserResponseDto> CheckUser(CheckUserRequestDto request)
        {
            var user = await _userRepository.GetQueryable()
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
                throw new UnAuthorizedException("Invalid username or password");

            var hashedPassword = _passwordService
                .HashPassword(request.Password, user.PasswordHash, out _);

            if (!hashedPassword.SequenceEqual(user.Password))
                throw new UnAuthorizedException("Invalid username or password");

            var tokenPayload = new TokenPayloadDto
            {
                UserId = user.Id,
                Username = user.Username,
                Role = user.Role
            };

            var token = _tokenService.CreateToken(tokenPayload);

            return new CheckUserResponseDto
            {
                Username = user.Username,
                Token = token
            };
        }

        public async Task RegisterUser(RegisterUserDto request)
        {
            var existingUser = await _userRepository.GetQueryable()
                .AnyAsync(u => u.Username == request.Username);

            if (existingUser)
                throw new BadRequestException("Username already exists");

            var hashedPassword = _passwordService
                .HashPassword(request.Password, null, out byte[] hashKey);

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                Password = hashedPassword,
                PasswordHash = hashKey,
                Role = "Creator"
            };

            await _userRepository.AddAsync(user);
        }
    }
}
