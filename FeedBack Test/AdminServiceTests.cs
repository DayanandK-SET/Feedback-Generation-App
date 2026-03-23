//using Feedback_Generation_App.Contexts;
//using Feedback_Generation_App.Exceptions;
//using Feedback_Generation_App.Interfaces;
//using Feedback_Generation_App.Models;
//using Feedback_Generation_App.Models.DTOs;
//using Feedback_Generation_App.Repositories;
//using Feedback_Generation_App.Services;
//using Microsoft.EntityFrameworkCore;

//namespace FeedbackBack_Unit_Tests
//{

//    // Tests: GetAllCreatorsAsync, GetAllSurveysAsync, DeleteSurveyAsync, DeleteCreatorAsync
//    // Pattern: Real Repository + InMemory DB

//    public class AdminServiceTests
//    {
//        private readonly FeedbackContext _context;
//        private readonly IRepository<int, User> _userRepository;
//        private readonly IRepository<int, Survey> _surveyRepository;
//        private readonly AdminService _adminService;

//        public AdminServiceTests()
//        {
//            var options = new DbContextOptionsBuilder<FeedbackContext>()
//                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
//                .Options;

//            _context = new FeedbackContext(options);
//            _userRepository = new Repository<int, User>(_context);
//            _surveyRepository = new Repository<int, Survey>(_context);

//            _adminService = new AdminService(_userRepository, _surveyRepository);
//        }

//        //  Helpers 

//        private async Task<User> AddUser(
//            string username,
//            string role = "Creator",
//            bool isDeleted = false)
//        {
//            var user = new User
//            {
//                Username = username,
//                Email = $"{username}@test.com",
//                Password = new byte[] { 1, 2, 3 },
//                PasswordHash = new byte[] { 4, 5, 6 },
//                Role = role,
//                IsDeleted = isDeleted
//            };
//            return (await _userRepository.AddAsync(user))!;
//        }

//        private async Task<Survey> AddSurvey(
//            string title,
//            int createdById,
//            bool isActive = true,
//            bool isDeleted = false)
//        {
//            var survey = new Survey
//            {
//                Title = title,
//                Description = "Test",
//                PublicIdentifier = Guid.NewGuid().ToString(),
//                IsActive = isActive,
//                CreatedById = createdById,
//                IsDeleted = isDeleted
//            };
//            return (await _surveyRepository.AddAsync(survey))!;
//        }


//        // GetAllCreatorsAsync Tests

//        [Fact]
//        public async Task GetAllCreatorsAsync_ReturnsOnlyCreatorRoleUsers()
//        {
//            // Arrange
//            await AddUser("creator1", "Creator");
//            await AddUser("creator2", "Creator");
//            await AddUser("adminuser", "Admin");   // should be excluded

//            // Act
//            var result = await _adminService.GetAllCreatorsAsync();

//            // Assert — only creators, not admin
//            Assert.Equal(2, result.Count);
//            Assert.All(result, c => Assert.DoesNotContain("admin", c.Username));
//        }

//        [Fact]
//        public async Task GetAllCreatorsAsync_ExcludesSoftDeletedCreators()
//        {
//            // Arrange
//            await AddUser("activecreator", "Creator", isDeleted: false);
//            await AddUser("deletedcreator", "Creator", isDeleted: true);

//            // Act
//            var result = await _adminService.GetAllCreatorsAsync();

//            // Assert — deleted creator not shown
//            Assert.Single(result);
//            Assert.Equal("activecreator", result[0].Username);
//        }

//        [Fact]
//        public async Task GetAllCreatorsAsync_NoCreators_ReturnsEmptyList()
//        {
//            // Arrange — only admin in DB
//            await AddUser("admin", "Admin");

//            // Act
//            var result = await _adminService.GetAllCreatorsAsync();

//            // Assert
//            Assert.Empty(result);
//        }

//        [Fact]
//        public async Task GetAllCreatorsAsync_ReturnsDtoWithCorrectFields()
//        {
//            // Arrange
//            await AddUser("testcreator", "Creator");

//            // Act
//            var result = await _adminService.GetAllCreatorsAsync();

//            // Assert — DTO has all required fields
//            Assert.Single(result);
//            Assert.True(result[0].Id > 0);
//            Assert.Equal("testcreator", result[0].Username);
//            Assert.Equal("testcreator@test.com", result[0].Email);
//        }


//        // GetAllSurveysAsync Tests

//        [Fact]
//        public async Task GetAllSurveysAsync_ReturnsAllNonDeletedSurveys()
//        {
//            // Arrange
//            var creator = await AddUser("surveycreator", "Creator");
//            await AddSurvey("Survey A", creator.Id);
//            await AddSurvey("Survey B", creator.Id);
//            await AddSurvey("Deleted Survey", creator.Id, isDeleted: true);

//            // Act
//            var result = await _adminService.GetAllSurveysAsync();

//            // Assert — only 2 non-deleted surveys
//            Assert.Equal(2, result.Count);
//            Assert.DoesNotContain(result, s => s.Title == "Deleted Survey");
//        }

//        [Fact]
//        public async Task GetAllSurveysAsync_ReturnsSurveysFromAllCreators()
//        {
//            // Arrange — two different creators
//            var creator1 = await AddUser("creator_a", "Creator");
//            var creator2 = await AddUser("creator_b", "Creator");
//            await AddSurvey("Creator A Survey", creator1.Id);
//            await AddSurvey("Creator B Survey", creator2.Id);

//            // Act
//            var result = await _adminService.GetAllSurveysAsync();

//            // Assert — admin sees all surveys
//            Assert.Equal(2, result.Count);
//        }

//        [Fact]
//        public async Task GetAllSurveysAsync_ReturnsDtoWithCreatorName()
//        {
//            // Arrange
//            var creator = await AddUser("myname", "Creator");
//            await AddSurvey("My Survey", creator.Id);

//            // Act
//            var result = await _adminService.GetAllSurveysAsync();

//            // Assert — creator name is populated in the DTO
//            Assert.Single(result);
//            Assert.Equal("myname", result[0].Creator);
//        }


//        // DeleteSurveyAsync Tests

//        [Fact]
//        public async Task DeleteSurveyAsync_ExistingSurvey_SoftDeletesSurvey()
//        {
//            // Arrange
//            var creator = await AddUser("delsurveyuser", "Creator");
//            var survey = await AddSurvey("To Be Deleted", creator.Id);

//            // Act
//            await _adminService.DeleteSurveyAsync(survey.Id);

//            // Assert — IsDeleted is now true, not physically removed
//            var fromDb = await _surveyRepository.GetQueryable()
//                .FirstOrDefaultAsync(s => s.Id == survey.Id);

//            Assert.NotNull(fromDb);
//            Assert.True(fromDb!.IsDeleted);
//        }

//        [Fact]
//        public async Task DeleteSurveyAsync_NonExistentSurveyId_ThrowsNotFoundException()
//        {
//            // Act & Assert
//            await Assert.ThrowsAsync<NotFoundException>(async () =>
//                await _adminService.DeleteSurveyAsync(surveyId: 9999)
//            );
//        }

//        [Fact]
//        public async Task DeleteSurveyAsync_AlreadyDeletedSurvey_ThrowsNotFoundException()
//        {
//            // Arrange
//            var creator = await AddUser("alreadydel", "Creator");
//            var survey = await AddSurvey("Already Deleted", creator.Id, isDeleted: true);

//            // Act & Assert — cannot delete something already deleted
//            await Assert.ThrowsAsync<NotFoundException>(async () =>
//                await _adminService.DeleteSurveyAsync(survey.Id)
//            );
//        }


//        // DeleteCreatorAsync Tests

//        [Fact]
//        public async Task DeleteCreatorAsync_ExistingCreator_SoftDeletesCreator()
//        {
//            // Arrange
//            var creator = await AddUser("todelete", "Creator");

//            // Act
//            await _adminService.DeleteCreatorAsync(creator.Id);

//            // Assert — soft deleted
//            var fromDb = await _userRepository.GetQueryable()
//                .FirstOrDefaultAsync(u => u.Id == creator.Id);

//            Assert.NotNull(fromDb);
//            Assert.True(fromDb!.IsDeleted);
//        }

//        [Fact]
//        public async Task DeleteCreatorAsync_NonExistentId_ThrowsNotFoundException()
//        {
//            // Act & Assert
//            await Assert.ThrowsAsync<NotFoundException>(async () =>
//                await _adminService.DeleteCreatorAsync(creatorId: 9999)
//            );
//        }

//        [Fact]
//        public async Task DeleteCreatorAsync_AdminUser_ThrowsNotFoundException()
//        {
//            // Arrange — try to delete an Admin (not a Creator)
//            var admin = await AddUser("sysadmin", "Admin");

//            // Act & Assert — admin accounts cannot be deleted via this method
//            await Assert.ThrowsAsync<NotFoundException>(async () =>
//                await _adminService.DeleteCreatorAsync(admin.Id)
//            );
//        }
//    }
//}


using Feedback_Generation_App.Contexts;
using Feedback_Generation_App.Exceptions;
using Feedback_Generation_App.Interfaces;
using Feedback_Generation_App.Models;
using Feedback_Generation_App.Models.DTOs;
using Feedback_Generation_App.Repositories;
using Feedback_Generation_App.Services;
using Microsoft.EntityFrameworkCore;

namespace FeedbackBack_Unit_Tests
{
    public class AdminServiceTests
    {
        private readonly FeedbackContext _context;
        private readonly IRepository<int, User> _userRepository;
        private readonly IRepository<int, Survey> _surveyRepository;
        private readonly IRepository<int, AuditLog> _auditLogRepository; 
        private readonly AdminService _adminService;

        public AdminServiceTests()
        {
            var options = new DbContextOptionsBuilder<FeedbackContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new FeedbackContext(options);
            _userRepository = new Repository<int, User>(_context);
            _surveyRepository = new Repository<int, Survey>(_context);
            _auditLogRepository = new Repository<int, AuditLog>(_context); 

            _adminService = new AdminService(
                _userRepository,
                _surveyRepository,
                _auditLogRepository  
            );
        }

        // Helpers
        private async Task<User> AddUser(
            string username,
            string role = "Creator",
            bool isDeleted = false)
        {
            var user = new User
            {
                Username = username,
                Email = $"{username}@test.com",
                Password = new byte[] { 1, 2, 3 },
                PasswordHash = new byte[] { 4, 5, 6 },
                Role = role,
                IsDeleted = isDeleted
            };
            return (await _userRepository.AddAsync(user))!;
        }

        private async Task<Survey> AddSurvey(
            string title,
            int createdById,
            bool isActive = true,
            bool isDeleted = false)
        {
            var survey = new Survey
            {
                Title = title,
                Description = "Test",
                PublicIdentifier = Guid.NewGuid().ToString(),
                IsActive = isActive,
                CreatedById = createdById,
                IsDeleted = isDeleted
            };
            return (await _surveyRepository.AddAsync(survey))!;
        }

        // GetAllCreatorsAsync Tests
        [Fact]
        public async Task GetAllCreatorsAsync_ReturnsOnlyCreatorRoleUsers()
        {
            await AddUser("creator1", "Creator");
            await AddUser("creator2", "Creator");
            await AddUser("adminuser", "Admin");

            var result = await _adminService.GetAllCreatorsAsync();

            Assert.Equal(2, result.Count);
            Assert.All(result, c => Assert.DoesNotContain("admin", c.Username));
        }

        [Fact]
        public async Task GetAllCreatorsAsync_ExcludesSoftDeletedCreators()
        {
            await AddUser("activecreator", "Creator", isDeleted: false);
            await AddUser("deletedcreator", "Creator", isDeleted: true);

            var result = await _adminService.GetAllCreatorsAsync();

            Assert.Single(result);
            Assert.Equal("activecreator", result[0].Username);
        }

        [Fact]
        public async Task GetAllCreatorsAsync_NoCreators_ReturnsEmptyList()
        {
            await AddUser("admin", "Admin");

            var result = await _adminService.GetAllCreatorsAsync();

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllCreatorsAsync_ReturnsDtoWithCorrectFields()
        {
            await AddUser("testcreator", "Creator");

            var result = await _adminService.GetAllCreatorsAsync();

            Assert.Single(result);
            Assert.True(result[0].Id > 0);
            Assert.Equal("testcreator", result[0].Username);
            Assert.Equal("testcreator@test.com", result[0].Email);
        }

        // GetAllSurveysAsync Tests
        [Fact]
        public async Task GetAllSurveysAsync_ReturnsAllNonDeletedSurveys()
        {
            var creator = await AddUser("surveycreator", "Creator");
            await AddSurvey("Survey A", creator.Id);
            await AddSurvey("Survey B", creator.Id);
            await AddSurvey("Deleted Survey", creator.Id, isDeleted: true);

            var result = await _adminService.GetAllSurveysAsync();

            Assert.Equal(2, result.Count);
            Assert.DoesNotContain(result, s => s.Title == "Deleted Survey");
        }

        [Fact]
        public async Task GetAllSurveysAsync_ReturnsSurveysFromAllCreators()
        {
            var creator1 = await AddUser("creator_a", "Creator");
            var creator2 = await AddUser("creator_b", "Creator");
            await AddSurvey("Creator A Survey", creator1.Id);
            await AddSurvey("Creator B Survey", creator2.Id);

            var result = await _adminService.GetAllSurveysAsync();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllSurveysAsync_ReturnsDtoWithCreatorName()
        {
            var creator = await AddUser("myname", "Creator");
            await AddSurvey("My Survey", creator.Id);

            var result = await _adminService.GetAllSurveysAsync();

            Assert.Single(result);
            Assert.Equal("myname", result[0].Creator);
        }

        // DeleteSurveyAsync Tests
        [Fact]
        public async Task DeleteSurveyAsync_ExistingSurvey_SoftDeletesSurvey()
        {
            var creator = await AddUser("delsurveyuser", "Creator");
            var survey = await AddSurvey("To Be Deleted", creator.Id);

            await _adminService.DeleteSurveyAsync(survey.Id);

            var fromDb = await _surveyRepository.GetQueryable()
                .FirstOrDefaultAsync(s => s.Id == survey.Id);
            Assert.NotNull(fromDb);
            Assert.True(fromDb!.IsDeleted);
        }

        [Fact]
        public async Task DeleteSurveyAsync_NonExistentSurveyId_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(async () =>
                await _adminService.DeleteSurveyAsync(surveyId: 9999)
            );
        }

        [Fact]
        public async Task DeleteSurveyAsync_AlreadyDeletedSurvey_ThrowsNotFoundException()
        {
            var creator = await AddUser("alreadydel", "Creator");
            var survey = await AddSurvey("Already Deleted", creator.Id, isDeleted: true);

            await Assert.ThrowsAsync<NotFoundException>(async () =>
                await _adminService.DeleteSurveyAsync(survey.Id)
            );
        }

        // DeleteCreatorAsync Tests
        [Fact]
        public async Task DeleteCreatorAsync_ExistingCreator_SoftDeletesCreator()
        {
            var creator = await AddUser("todelete", "Creator");

            await _adminService.DeleteCreatorAsync(creator.Id);

            var fromDb = await _userRepository.GetQueryable()
                .FirstOrDefaultAsync(u => u.Id == creator.Id);
            Assert.NotNull(fromDb);
            Assert.True(fromDb!.IsDeleted);
        }

        [Fact]
        public async Task DeleteCreatorAsync_NonExistentId_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(async () =>
                await _adminService.DeleteCreatorAsync(creatorId: 9999)
            );
        }

        [Fact]
        public async Task DeleteCreatorAsync_AdminUser_ThrowsNotFoundException()
        {
            var admin = await AddUser("sysadmin", "Admin");

            await Assert.ThrowsAsync<NotFoundException>(async () =>
                await _adminService.DeleteCreatorAsync(admin.Id)
            );
        }
    }
}
