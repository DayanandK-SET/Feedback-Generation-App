using Feedback_Generation_App.Contexts;
using Feedback_Generation_App.Exceptions;
using Feedback_Generation_App.Interfaces;
using Feedback_Generation_App.Models;
using Feedback_Generation_App.Models.DTOs;
using Feedback_Generation_App.Repositories;
using Feedback_Generation_App.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FeedbackBack_Unit_Tests
{

    // Tests: RegisterUser, CheckUser
    // Pattern: Real Repository + InMemory DB + Mock external services

    public class UserServiceTests
    {
        private readonly FeedbackContext _context;
        private readonly IRepository<int, User> _userRepository;
        private readonly PasswordService _passwordService;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly UserService _userService;

        // Constructor runs fresh before Every test (xUnit)
        // Guid.NewGuid() ensures each test gets a completely clean DB
        public UserServiceTests()
        {
            var options = new DbContextOptionsBuilder<FeedbackContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new FeedbackContext(options);
            _userRepository = new Repository<int, User>(_context);

            // Real PasswordService — no external dependencies
            _passwordService = new PasswordService();

            // Mock TokenService
            _mockTokenService = new Mock<ITokenService>();
            _mockTokenService
                .Setup(ts => ts.CreateToken(It.IsAny<TokenPayloadDto>()))
                .Returns("mock-jwt-token");

            _userService = new UserService(
                _userRepository,
                _passwordService,
                _mockTokenService.Object
            );
        }

        // Helper 
        // Registers a user and returns the saved entity from DB
        private async Task<User> RegisterTestUser(
            string username = "testuser",
            string password = "Test@123")
        {
            await _userService.RegisterUser(new RegisterUserDto
            {
                Username = username,
                Email = $"{username}@test.com",
                Password = password
            });

            return await _userRepository.GetQueryable()
                .FirstAsync(u => u.Username == username);
        }


        // RegisterUser Tests

        [Fact]
        public async Task RegisterUser_ValidDto_UserSavedToDatabase()
        {
            // Arrange
            var dto = new RegisterUserDto
            {
                Username = "newcreator",
                Email = "newcreator@test.com",
                Password = "Test@123"
            };

            // Act
            await _userService.RegisterUser(dto);

            // Assert
            var user = await _userRepository.GetQueryable()
                .FirstOrDefaultAsync(u => u.Username == "newcreator");

            Assert.NotNull(user);
            Assert.Equal("newcreator", user.Username);
            Assert.Equal("newcreator@test.com", user.Email);
        }

        [Fact]
        public async Task RegisterUser_NewUser_RoleIsAlwaysCreator()
        {
            // Arrange
            var dto = new RegisterUserDto
            {
                Username = "rolecheck",
                Email = "role@test.com",
                Password = "Test@123"
            };

            // Act
            await _userService.RegisterUser(dto);

            // Assert — role must always be "Creator", never "Admin"
            var user = await _userRepository.GetQueryable()
                .FirstAsync(u => u.Username == "rolecheck");

            Assert.Equal("Creator", user.Role);
        }

        [Fact]
        public async Task RegisterUser_NewUser_PasswordIsHashedNotPlainText()
        {
            // Arrange
            var dto = new RegisterUserDto
            {
                Username = "hashuser",
                Email = "hash@test.com",
                Password = "PlainTextPassword"
            };

            // Act
            await _userService.RegisterUser(dto);

            // Assert — stored password should NOT match raw string bytes
            var user = await _userRepository.GetQueryable()
                .FirstAsync(u => u.Username == "hashuser");

            var plainBytes = System.Text.Encoding.UTF8.GetBytes("PlainTextPassword");

            Assert.False(user.Password.SequenceEqual(plainBytes),
                "Password should be hashed, not stored as plain text");
            Assert.NotEmpty(user.PasswordHash);
        }

        [Fact]
        public async Task RegisterUser_DuplicateUsername_ThrowsException()
        {
            // Arrange — register first user
            await RegisterTestUser("existinguser");

            // Act & Assert — register same username again
            await Assert.ThrowsAsync<BadRequestException>(async () =>
                await _userService.RegisterUser(new RegisterUserDto
                {
                    Username = "existinguser",
                    Email = "another@test.com",
                    Password = "Test@123"
                })
            );
        }


        // CheckUser Tests

        [Fact]
        public async Task CheckUser_ValidCredentials_ReturnsUsernameAndToken()
        {
            // Arrange
            await RegisterTestUser("loginuser", "MyPass@123");

            var request = new CheckUserRequestDto
            {
                Username = "loginuser",
                Password = "MyPass@123"
            };

            // Act
            var result = await _userService.CheckUser(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("loginuser", result.Username);
            Assert.Equal("mock-jwt-token", result.Token);
        }

        [Fact]
        public async Task CheckUser_ValidLogin_TokenServiceIsCalledOnce()
        {
            // Arrange
            await RegisterTestUser("tokenverify", "Test@123");

            var request = new CheckUserRequestDto
            {
                Username = "tokenverify",
                Password = "Test@123"
            };

            // Act
            await _userService.CheckUser(request);

            // Assert — token service must be called exactly once per login
            _mockTokenService.Verify(
                ts => ts.CreateToken(It.IsAny<TokenPayloadDto>()),
                Times.Once
            );
        }

        [Fact]
        public async Task CheckUser_UsernameDoesNotExist_ThrowsUnAuthorizedException()
        {
            // Arrange — no user registered
            var request = new CheckUserRequestDto
            {
                Username = "ghostuser",
                Password = "Test@123"
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnAuthorizedException>(async () =>
                await _userService.CheckUser(request)
            );
        }

        [Fact]
        public async Task CheckUser_WrongPassword_ThrowsUnAuthorizedException()
        {
            // Arrange
            await RegisterTestUser("pwdtest", "CorrectPass@123");

            var request = new CheckUserRequestDto
            {
                Username = "pwdtest",
                Password = "WrongPass@999"
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnAuthorizedException>(async () =>
                await _userService.CheckUser(request)
            );
        }

        [Fact]
        public async Task CheckUser_WrongPassword_TokenServiceIsNeverCalled()
        {
            // Arrange
            await RegisterTestUser("notokentest", "CorrectPass@123");

            // Act — wrong password
            try
            {
                await _userService.CheckUser(new CheckUserRequestDto
                {
                    Username = "notokentest",
                    Password = "WrongPass"
                });
            }
            catch (UnAuthorizedException) { }

            // Assert — token must NOT be created on failed login
            _mockTokenService.Verify(
                ts => ts.CreateToken(It.IsAny<TokenPayloadDto>()),
                Times.Never
            );
        }
    }
}
