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

            await _adminService.ToggleCreatorStatusAsync(creator.Id);

            var fromDb = await _userRepository.GetQueryable()
                .FirstOrDefaultAsync(u => u.Id == creator.Id);
            Assert.NotNull(fromDb);
            Assert.True(fromDb!.IsDeleted);
        }

        [Fact]
        public async Task DeleteCreatorAsync_NonExistentId_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(async () =>
                await _adminService.ToggleCreatorStatusAsync(creatorId: 9999)
            );
        }

        [Fact]
        public async Task DeleteCreatorAsync_AdminUser_ThrowsNotFoundException()
        {
            var admin = await AddUser("sysadmin", "Admin");

            await Assert.ThrowsAsync<NotFoundException>(async () =>
                await _adminService.ToggleCreatorStatusAsync(admin.Id)
            );
        }



        [Fact]
        public async Task ToggleCreatorStatusAsync_DeletedCreator_ActivatesCreator()
        {
            var creator = await AddUser("inactive", "Creator", isDeleted: true);

            await _adminService.ToggleCreatorStatusAsync(creator.Id);

            var fromDb = await _userRepository.GetQueryable()
                .FirstAsync(u => u.Id == creator.Id);

            Assert.False(fromDb.IsDeleted);
        }

        [Fact]
        public async Task GetAuditLogsAsync_ReturnsLogsOrderedByPerformedAtDesc()
        {
            await _auditLogRepository.AddAsync(new AuditLog
            {
                Action = "Old",
                SurveyTitle = "Old",
                PerformedBy = "Admin",
                PerformedAt = DateTime.UtcNow.AddMinutes(-10)
            });

            await _auditLogRepository.AddAsync(new AuditLog
            {
                Action = "New",
                SurveyTitle = "New",
                PerformedBy = "Admin",
                PerformedAt = DateTime.UtcNow
            });

            var result = await _adminService.GetAuditLogsAsync();

            Assert.Equal(2, result.Count);
            Assert.Equal("New", result[0].Action);
        }


        [Fact]
        public async Task SearchCreatorsAsync_WithSearchText_ReturnsFilteredCreators()
        {
            await AddUser("alpha");
            await AddUser("beta");

            var result = await _adminService.SearchCreatorsAsync(
                new GetAdminCreatorsRequestDto { Search = "alp" });

            Assert.Single(result.Creators);
            Assert.Equal("alpha", result.Creators[0].Username);
        }

        [Fact]
        public async Task SearchCreatorsAsync_IsActiveFalse_ReturnsDeletedCreators()
        {
            await AddUser("active", isDeleted: false);
            await AddUser("inactive", isDeleted: true);

            var result = await _adminService.SearchCreatorsAsync(
                new GetAdminCreatorsRequestDto { IsActive = false });

            Assert.Single(result.Creators);
            Assert.Equal("inactive", result.Creators[0].Username);
        }


        [Fact]
        public async Task SearchCreatorsAsync_Pagination_WorksCorrectly()
        {
            await AddUser("a");
            await AddUser("b");
            await AddUser("c");

            var result = await _adminService.SearchCreatorsAsync(
                new GetAdminCreatorsRequestDto
                {
                    PageNumber = 1,
                    PageSize = 2
                });

            Assert.Equal(2, result.Creators.Count);
            Assert.Equal(3, result.TotalAllCreators);
        }



        [Fact]
        public async Task SearchSurveysAsync_SearchByTitle_ReturnsMatchingSurvey()
        {
            var creator = await AddUser("creator");
            await AddSurvey("Employee Feedback", creator.Id);
            await AddSurvey("Customer Review", creator.Id);

            var result = await _adminService.SearchSurveysAsync(
                new GetAdminSurveysRequestDto { Search = "Employee" });

            Assert.Single(result.Surveys);
        }



        [Fact]
        public async Task SearchSurveysAsync_FilterByCreator()
        {
            var a = await AddUser("alice");
            var b = await AddUser("bob");

            await AddSurvey("A Survey", a.Id);
            await AddSurvey("B Survey", b.Id);

            var result = await _adminService.SearchSurveysAsync(
                new GetAdminSurveysRequestDto { Creator = "alice" });

            Assert.Single(result.Surveys);
            Assert.Contains("alice", result.Surveys[0].Creator);
        }


        [Fact]
        public async Task SearchSurveysAsync_FilterByIsActive()
        {
            var creator = await AddUser("creator");
            await AddSurvey("Active", creator.Id, isActive: true);
            await AddSurvey("Inactive", creator.Id, isActive: false);

            var result = await _adminService.SearchSurveysAsync(
                new GetAdminSurveysRequestDto { IsActive = false });

            Assert.Single(result.Surveys);
            Assert.False(result.Surveys[0].IsActive);
        }

    }
}
