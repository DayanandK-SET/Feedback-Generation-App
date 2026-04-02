using Feedback_Generation_App.Contexts;
using Feedback_Generation_App.Exceptions;
using Feedback_Generation_App.Interfaces;
using Feedback_Generation_App.Models;
using Feedback_Generation_App.Models.DTOs;
using Feedback_Generation_App.Repositories;
using Feedback_Generation_App.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace FeedbackBack_Unit_Tests
{
    public class SurveyServiceTests
    {
        private readonly FeedbackContext _context;
        private readonly IRepository<int, Survey> _surveyRepository;
        private readonly IRepository<int, QuestionBank> _bankRepository;
        private readonly IRepository<int, Response> _responseRepository;
        private readonly IRepository<int, AuditLog> _auditLogRepository;

        private readonly IRepository<int, User> _userRepository;


        public SurveyServiceTests()
        {
            var options = new DbContextOptionsBuilder<FeedbackContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new FeedbackContext(options);
            _surveyRepository = new Repository<int, Survey>(_context);
            _bankRepository = new Repository<int, QuestionBank>(_context);
            _responseRepository = new Repository<int, Response>(_context);
            _auditLogRepository = new Repository<int, AuditLog>(_context);
            _userRepository = new Repository<int, User>(_context);
        }

        // Creates a SurveyService with the given role (Creator or Admin)
        private SurveyService CreateService(string role = "Creator")
        {
            var mockAccessor = new Mock<IHttpContextAccessor>();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "testuser")
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };

            mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);


            return new SurveyService(
                mockAccessor.Object,
                _surveyRepository,
                _bankRepository,
                _responseRepository,
                _auditLogRepository,
                _userRepository
            );

        }

        // Adds a survey to the InMemory DB directly
        private async Task<Survey> AddSurvey(
            int createdById = 1,
            bool isActive = true,
            bool isDeleted = false,
            string title = "Test Survey")
        {
            var survey = new Survey
            {
                Title = title,
                Description = "Test Description",
                PublicIdentifier = Guid.NewGuid().ToString(),
                IsActive = isActive,
                CreatedById = createdById,
                IsDeleted = isDeleted,
                Questions = new List<Question>
                {
                    new Question
                    {
                        Text = "Sample question",
                        QuestionType = QuestionType.Text
                    }
                }
            };
            return (await _surveyRepository.AddAsync(survey))!;
        }

        // Adds a Response with answers for a survey
        private async Task AddResponse(int surveyId, int questionId)
        {
            var response = new Response
            {
                SurveyId = surveyId,
                ResponseToken = Guid.NewGuid().ToString(),
                Answers = new List<Answer>
                {
                    new Answer
                    {
                        QuestionId = questionId,
                        AnswerText = "Test answer"
                    }
                }
            };
            await _responseRepository.AddAsync(response);
        }

        // CreateSurvey Tests
        [Fact]
        public async Task CreateSurvey_ManualTextQuestion_SurveySavedAndReturnsIdentifier()
        {
            var service = CreateService("Creator");
            var dto = new CreateSurveyDto
            {
                Title = "My Survey",
                Description = "Testing",
                Questions = new List<CreateQuestionDto>
                {
                    new CreateQuestionDto { Text = "How are you?", QuestionType = QuestionType.Text }
                }
            };

            var publicId = await service.CreateSurvey(dto, creatorId: 1);

            Assert.NotNull(publicId);
            Assert.NotEmpty(publicId);
            var saved = await _surveyRepository.GetQueryable()
                .FirstOrDefaultAsync(s => s.PublicIdentifier == publicId);
            Assert.NotNull(saved);
            Assert.Equal("My Survey", saved!.Title);
        }

        [Fact]
        public async Task CreateSurvey_WithQuestionBankReference_UsesBankQuestionText()
        {
            var service = CreateService("Creator");
            var bankQuestion = new QuestionBank
            {
                Text = "Bank Question Text",
                QuestionType = QuestionType.Rating,
                CreatedById = 1
            };
            await _bankRepository.AddAsync(bankQuestion);

            var dto = new CreateSurveyDto
            {
                Title = "Survey from Bank",
                Description = "Uses bank question",
                Questions = new List<CreateQuestionDto>
                {
                    new CreateQuestionDto { QuestionBankId = bankQuestion.Id }
                }
            };

            var publicId = await service.CreateSurvey(dto, creatorId: 1);

            var saved = await _surveyRepository.GetQueryable()
                .Include(s => s.Questions)
                .FirstOrDefaultAsync(s => s.PublicIdentifier == publicId);
            Assert.NotNull(saved);
            Assert.Single(saved!.Questions!);
            Assert.Equal("Bank Question Text", saved.Questions!.First().Text);
        }

        [Fact]
        public async Task CreateSurvey_InvalidQuestionBankId_ThrowsBadRequestException()
        {
            var service = CreateService("Creator");
            var dto = new CreateSurveyDto
            {
                Title = "Bad Survey",
                Description = "Will fail",
                Questions = new List<CreateQuestionDto>
                {
                    new CreateQuestionDto { QuestionBankId = 9999 }
                }
            };

            await Assert.ThrowsAsync<BadRequestException>(async () =>
                await service.CreateSurvey(dto, creatorId: 1)
            );
        }

        [Fact]
        public async Task CreateSurvey_WithExpiryAndMaxResponses_FieldsAreSaved()
        {
            var service = CreateService("Creator");
            var expiry = DateTime.UtcNow.AddDays(7);
            var dto = new CreateSurveyDto
            {
                Title = "Limited Survey",
                Description = "Has expiry and max",
                ExpireAt = expiry,
                MaxResponses = 50,
                Questions = new List<CreateQuestionDto>
                {
                    new CreateQuestionDto { Text = "Q1", QuestionType = QuestionType.Text }
                }
            };

            var publicId = await service.CreateSurvey(dto, creatorId: 1);

            var saved = await _surveyRepository.GetQueryable()
                .FirstAsync(s => s.PublicIdentifier == publicId);
            Assert.Equal(50, saved.MaxResponses);
            Assert.NotNull(saved.ExpireAt);
        }

        // DeleteSurveyAsync Tests
        [Fact]
        public async Task DeleteSurveyAsync_OwnSurvey_SurveyIsSoftDeleted()
        {
            var service = CreateService("Creator");
            var survey = await AddSurvey(createdById: 1);

            await service.DeleteSurveyAsync(survey.Id, userId: 1);

            var fromDb = await _surveyRepository.GetQueryable()
                .FirstOrDefaultAsync(s => s.Id == survey.Id);
            Assert.NotNull(fromDb);
            Assert.True(fromDb!.IsDeleted);
        }

        [Fact]
        public async Task DeleteSurveyAsync_NonExistentSurvey_ThrowsNotFoundException()
        {
            var service = CreateService("Creator");

            await Assert.ThrowsAsync<NotFoundException>(async () =>
                await service.DeleteSurveyAsync(surveyId: 9999, userId: 1)
            );
        }

        [Fact]
        public async Task DeleteSurveyAsync_SurveyBelongsToAnotherUser_ThrowsForbiddenException()
        {
            var service = CreateService("Creator");
            var survey = await AddSurvey(createdById: 99);

            await Assert.ThrowsAsync<ForbiddenException>(async () =>
                await service.DeleteSurveyAsync(survey.Id, userId: 1)
            );
        }

        [Fact]
        public async Task DeleteSurveyAsync_AdminCanDeleteAnySurvey()
        {
            var service = CreateService("Admin");
            var survey = await AddSurvey(createdById: 99);

            await service.DeleteSurveyAsync(survey.Id, userId: 1);

            var fromDb = await _surveyRepository.GetQueryable()
                .FirstOrDefaultAsync(s => s.Id == survey.Id);
            Assert.True(fromDb!.IsDeleted);
        }

        // ToggleSurveyStatusAsync Tests
        [Fact]
        public async Task ToggleSurveyStatusAsync_ActiveSurvey_BecomesInactive()
        {
            var service = CreateService("Creator");
            var survey = await AddSurvey(createdById: 1, isActive: true);

            await service.ToggleSurveyStatusAsync(survey.Id, userId: 1);

            var fromDb = await _surveyRepository.GetQueryable()
                .FirstAsync(s => s.Id == survey.Id);
            Assert.False(fromDb.IsActive);
        }

        [Fact]
        public async Task ToggleSurveyStatusAsync_InactiveSurvey_BecomesActive()
        {
            var service = CreateService("Creator");
            var survey = await AddSurvey(createdById: 1, isActive: false);

            await service.ToggleSurveyStatusAsync(survey.Id, userId: 1);

            var fromDb = await _surveyRepository.GetQueryable()
                .FirstAsync(s => s.Id == survey.Id);
            Assert.True(fromDb.IsActive);
        }

        [Fact]
        public async Task ToggleSurveyStatusAsync_OtherUserSurvey_ThrowsForbiddenException()
        {
            var service = CreateService("Creator");
            var survey = await AddSurvey(createdById: 99);

            await Assert.ThrowsAsync<ForbiddenException>(async () =>
                await service.ToggleSurveyStatusAsync(survey.Id, userId: 1)
            );
        }

        // UpdateSurveyAsync Tests
        [Fact]
        public async Task UpdateSurveyAsync_ValidDto_TitleAndDescriptionUpdated()
        {
            var service = CreateService("Creator");
            var survey = await AddSurvey(createdById: 1, title: "Old Title");
            var dto = new UpdateSurveyDto { Title = "New Title", Description = "New Description" };

            await service.UpdateSurveyAsync(survey.Id, userId: 1, dto);

            var fromDb = await _surveyRepository.GetQueryable()
                .FirstAsync(s => s.Id == survey.Id);
            Assert.Equal("New Title", fromDb.Title);
            Assert.Equal("New Description", fromDb.Description);
        }

        [Fact]
        public async Task UpdateSurveyAsync_EmptyTitle_ThrowsBadRequestException()
        {
            var service = CreateService("Creator");
            var survey = await AddSurvey(createdById: 1);
            var dto = new UpdateSurveyDto { Title = "", Description = "Fine" };

            await Assert.ThrowsAsync<BadRequestException>(async () =>
                await service.UpdateSurveyAsync(survey.Id, userId: 1, dto)
            );
        }

        [Fact]
        public async Task UpdateSurveyAsync_WhitespaceTitle_ThrowsBadRequestException()
        {
            var service = CreateService("Creator");
            var survey = await AddSurvey(createdById: 1);
            var dto = new UpdateSurveyDto { Title = " ", Description = "Fine" };

            await Assert.ThrowsAsync<BadRequestException>(async () =>
                await service.UpdateSurveyAsync(survey.Id, userId: 1, dto)
            );
        }

        [Fact]
        public async Task UpdateSurveyAsync_OtherUserSurvey_ThrowsForbiddenException()
        {
            var service = CreateService("Creator");
            var survey = await AddSurvey(createdById: 99);
            var dto = new UpdateSurveyDto { Title = "Hacked Title", Description = "" };

            await Assert.ThrowsAsync<ForbiddenException>(async () =>
                await service.UpdateSurveyAsync(survey.Id, userId: 1, dto)
            );
        }

        // GetSurveyResponsesAsync Tests
        [Fact]
        public async Task GetSurveyResponsesAsync_SurveyWithResponses_ReturnsCorrectCount()
        {
            var service = CreateService("Creator");
            var survey = await AddSurvey(createdById: 1);
            var question = await _context.Set<Question>()
                .FirstAsync(q => q.SurveyId == survey.Id);

            await AddResponse(survey.Id, question.Id);
            await AddResponse(survey.Id, question.Id);

            var request = new GetSurveyResponsesRequestDto { PageNumber = 1, PageSize = 10 };

            var result = await service.GetSurveyResponsesAsync(survey.Id, userId: 1, request);

            Assert.Equal(survey.Id, result.SurveyId);
            Assert.Equal(2, result.TotalResponses);
        }

        [Fact]
        public async Task GetSurveyResponsesAsync_NonExistentSurvey_ThrowsNotFoundException()
        {
            var service = CreateService("Creator");
            var request = new GetSurveyResponsesRequestDto { PageNumber = 1, PageSize = 10 };

            await Assert.ThrowsAsync<NotFoundException>(async () =>
                await service.GetSurveyResponsesAsync(9999, userId: 1, request)
            );
        }

        [Fact]
        public async Task GetSurveyResponsesAsync_OtherUserSurvey_ThrowsForbiddenException()
        {
            var service = CreateService("Creator");
            var survey = await AddSurvey(createdById: 99);
            var request = new GetSurveyResponsesRequestDto { PageNumber = 1, PageSize = 10 };

            await Assert.ThrowsAsync<ForbiddenException>(async () =>
                await service.GetSurveyResponsesAsync(survey.Id, userId: 1, request)
            );
        }

        // GetSurveyResponseTrendAsync Tests
        [Fact]
        public async Task GetSurveyResponseTrendAsync_WithResponses_ReturnsTrendGroupedByDate()
        {
            var service = CreateService("Creator");
            var survey = await AddSurvey(createdById: 1);
            var question = await _context.Set<Question>()
                .FirstAsync(q => q.SurveyId == survey.Id);

            await AddResponse(survey.Id, question.Id);
            await AddResponse(survey.Id, question.Id);

            var result = await service.GetSurveyResponseTrendAsync(survey.Id, userId: 1);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.All(result, r =>
            {
                Assert.NotEmpty(r.Date);
                Assert.True(r.Count > 0);
            });
        }

        [Fact]
        public async Task GetSurveyResponseTrendAsync_NoResponses_ReturnsEmptyList()
        {
            var service = CreateService("Creator");
            var survey = await AddSurvey(createdById: 1);

            var result = await service.GetSurveyResponseTrendAsync(survey.Id, userId: 1);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetSurveyResponseTrendAsync_NonExistentSurvey_ThrowsNotFoundException()
        {
            var service = CreateService("Creator");

            await Assert.ThrowsAsync<NotFoundException>(async () =>
                await service.GetSurveyResponseTrendAsync(9999, userId: 1)
            );
        }

        // GetSurveyAnalyticsAsync Tests
        [Fact]
        public async Task GetSurveyAnalyticsAsync_ValidSurvey_ReturnsSurveyIdAndTitle()
        {
            var service = CreateService("Creator");
            var survey = await AddSurvey(createdById: 1, title: "Analytics Survey");

            var result = await service.GetSurveyAnalyticsAsync(survey.Id, userId: 1);

            Assert.NotNull(result);
            Assert.Equal(survey.Id, result.SurveyId);
            Assert.Equal("Analytics Survey", result.Title);
        }

        [Fact]
        public async Task GetSurveyAnalyticsAsync_NonExistentSurvey_ThrowsException()
        {
            var service = CreateService("Creator");

            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.GetSurveyAnalyticsAsync(9999, userId: 1)
            );
        }

        [Fact]
        public async Task GetSurveyAnalyticsAsync_OtherUserSurvey_ThrowsException()
        {
            var service = CreateService("Creator");
            var survey = await AddSurvey(createdById: 99);

            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.GetSurveyAnalyticsAsync(survey.Id, userId: 1)
            );
        }


        [Fact]
        public async Task CreateSurvey_DeletedCreator_ThrowsForbiddenException()
        {
            await _userRepository.AddAsync(new User
            {
                Id = 5,
                Username = "deleted",
                IsDeleted = true
            });

            var service = CreateService("Creator");

            var dto = new CreateSurveyDto
            {
                Title = "Blocked",
                Questions = new()
        {
            new CreateQuestionDto { Text = "Q", QuestionType = QuestionType.Text }
        }
            };

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.CreateSurvey(dto, creatorId: 5));
        }


        [Fact]
        public async Task ExportResponsesToExcelAsync_ValidSurvey_ReturnsExcelBytes()
        {
            var service = CreateService("Creator");
            var survey = await AddSurvey(createdById: 1);
            var question = await _context.Set<Question>()
                .FirstAsync(q => q.SurveyId == survey.Id);

            await AddResponse(survey.Id, question.Id);

            var bytes = await service.ExportResponsesToExcelAsync(survey.Id, userId: 1);

            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);
        }


        [Fact]
        public async Task ExportResponsesToExcelAsync_NonExistentSurvey_ThrowsNotFound()
        {
            var service = CreateService("Creator");

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.ExportResponsesToExcelAsync(9999, 1));
        }



        [Fact]
        public async Task ImportSurveyFromExcelAsync_ValidExcel_CreatesSurvey()
        {
            var service = CreateService("Creator");

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.AddWorksheet("Questions");

            ws.Cell(1, 1).Value = "Question";
            ws.Cell(1, 2).Value = "Type";
            ws.Cell(2, 1).Value = "How are you?";
            ws.Cell(2, 2).Value = "Text";

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var file = new FormFile(stream, 0, stream.Length, "file", "survey.xlsx");

            var dto = new ImportSurveyExcelDto
            {
                Title = "Imported Survey",
                File = file
            };

            var publicId = await service.ImportSurveyFromExcelAsync(dto, creatorId: 1);

            Assert.NotEmpty(publicId);
        }


        [Fact]
        public async Task ImportSurveyFromExcelAsync_NullFile_ThrowsBadRequest()
        {
            var service = CreateService("Creator");

            var dto = new ImportSurveyExcelDto { Title = "Fail" };

            await Assert.ThrowsAsync<BadRequestException>(() =>
                service.ImportSurveyFromExcelAsync(dto, 1));
        }


        [Fact]
        public async Task GetCreatorSurveysAsync_ExpiredSurvey_AutoDeactivates()
        {
            var service = CreateService("Creator");

            var survey = await _surveyRepository.AddAsync(new Survey
            {
                Title = "Expired",
                CreatedById = 1,
                IsActive = true,
                ExpireAt = DateTime.UtcNow.AddDays(-1)
            });

            var result = await service.GetCreatorSurveysAsync(1, new GetMySurveysRequestDto());

            Assert.False(result.Surveys.First().IsActive);
        }


        [Fact]
        public async Task GetCreatorSurveysAsync_FilterByIsActive_ReturnsOnlyActive()
        {
            var service = CreateService("Creator");

            await AddSurvey(createdById: 1, isActive: true);
            await AddSurvey(createdById: 1, isActive: false);

            var result = await service.GetCreatorSurveysAsync(
                1,
                new GetMySurveysRequestDto { IsActive = true });

            Assert.Single(result.Surveys);
            Assert.True(result.Surveys[0].IsActive);
        }

        [Fact]
        public async Task ImportSurveyFromExcel_MCQuestionWithoutOptions_ThrowsBadRequest()
        {
            var service = CreateService("Creator");

            using var wb = new ClosedXML.Excel.XLWorkbook();
            var ws = wb.AddWorksheet("Sheet1");

            ws.Cell(1, 1).Value = "Question";
            ws.Cell(1, 2).Value = "Type";
            ws.Cell(2, 1).Value = "Pick one";
            ws.Cell(2, 2).Value = "MultipleChoice";

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;

            var file = new FormFile(stream, 0, stream.Length, "file", "bad.xlsx");

            var dto = new ImportSurveyExcelDto { Title = "Bad", File = file };

            await Assert.ThrowsAsync<BadRequestException>(() =>
                service.ImportSurveyFromExcelAsync(dto, 1));
        }


        [Fact]
        public async Task GetCreatorSurveysAsync_MaxResponsesReached_AutoDeactivates()
        {
            var service = CreateService("Creator");

            var survey = await _surveyRepository.AddAsync(new Survey
            {
                Title = "Limited",
                CreatedById = 1,
                IsActive = true,
                MaxResponses = 1,
                Responses = new List<Response>
        {
            new Response { IsDeleted = false }
        }
            });

            var result = await service.GetCreatorSurveysAsync(
                1,
                new GetMySurveysRequestDto());

            Assert.False(result.Surveys.First().IsActive);
        }


        [Fact]
        public async Task GetSurveyResponsesAsync_WithDateFilters_FiltersCorrectly()
        {
            var service = CreateService("Creator");
            var survey = await AddSurvey(1);

            var question = await _context.Set<Question>()
                .FirstAsync(q => q.SurveyId == survey.Id);

            await AddResponse(survey.Id, question.Id);
            await AddResponse(survey.Id, question.Id);

            var request = new GetSurveyResponsesRequestDto
            {
                FromDate = DateTime.UtcNow.AddMinutes(-1),
                ToDate = DateTime.UtcNow.AddMinutes(1)
            };

            var result = await service.GetSurveyResponsesAsync(survey.Id, 1, request);

            Assert.True(result.TotalResponses >= 1);
        }


        [Fact]
        public async Task CreateSurvey_ManualQuestion_MissingTextOrType_ThrowsBadRequest()
        {
            var service = CreateService("Creator");

            var dto = new CreateSurveyDto
            {
                Title = "Invalid",
                Questions = new()
        {
            new CreateQuestionDto { Text = "", QuestionType = null }
        }
            };

            await Assert.ThrowsAsync<BadRequestException>(() =>
                service.CreateSurvey(dto, 1));
        }



        [Fact]
        public async Task CreateSurvey_DeletedBankQuestion_ThrowsBadRequest()
        {
            var service = CreateService("Creator");

            var bank = await _bankRepository.AddAsync(new QuestionBank
            {
                Text = "Deleted",
                QuestionType = QuestionType.Text,
                CreatedById = 1,
                IsDeleted = true
            });

            var dto = new CreateSurveyDto
            {
                Title = "Fail",
                Questions = new()
        {
            new CreateQuestionDto { QuestionBankId = bank.Id }
        }
            };

            await Assert.ThrowsAsync<BadRequestException>(() =>
                service.CreateSurvey(dto, 1));
        }



        [Fact]
        public async Task DeleteSurveyAsync_AlreadyDeletedSurvey_ThrowsNotFound()
        {
            var service = CreateService("Creator");

            var survey = await AddSurvey(1, isDeleted: true);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.DeleteSurveyAsync(survey.Id, 1));
        }




        [Fact]
        public async Task ToggleSurveyStatusAsync_DeletedSurvey_ThrowsNotFound()
        {
            var service = CreateService("Creator");

            var survey = await AddSurvey(1, isDeleted: true);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.ToggleSurveyStatusAsync(survey.Id, 1));
        }




        [Fact]
        public async Task UpdateSurveyAsync_NoFieldsProvided_ThrowsBadRequest()
        {
            var service = CreateService("Creator");
            var survey = await AddSurvey(1);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                service.UpdateSurveyAsync(survey.Id, 1, new UpdateSurveyDto()));
        }



        [Fact]
        public async Task ExportResponsesToExcelAsync_NoResponses_StillReturnsFile()
        {
            var service = CreateService("Creator");
            var survey = await AddSurvey(1);

            var bytes = await service.ExportResponsesToExcelAsync(survey.Id, 1);

            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);
        }


        [Fact]
        public async Task GetCreatorSurveysAsync_NoSurveys_ReturnsEmpty()
        {
            var service = CreateService("Creator");

            var result = await service.GetCreatorSurveysAsync(
                1,
                new GetMySurveysRequestDto());

            Assert.Empty(result.Surveys);
        }


        [Fact]
        public async Task CreateSurvey_NonExistentCreator_AllowsSurveyCreation()
        {
            var service = CreateService("Creator");

            var dto = new CreateSurveyDto
            {
                Title = "Valid",
                Questions = new()
        {
            new CreateQuestionDto { Text = "Q1", QuestionType = QuestionType.Text }
        }
            };

            var publicId = await service.CreateSurvey(dto, creatorId: 999);

            Assert.NotEmpty(publicId);
        }

        [Fact]
        public async Task GetSurveyResponsesAsync_OnlyFromDate_Works()
        {
            var service = CreateService("Creator");
            var survey = await AddSurvey(1);

            var question = await _context.Set<Question>()
                .FirstAsync(q => q.SurveyId == survey.Id);

            await AddResponse(survey.Id, question.Id);

            var result = await service.GetSurveyResponsesAsync(
                survey.Id,
                1,
                new GetSurveyResponsesRequestDto
                {
                    FromDate = DateTime.UtcNow.AddMinutes(-5)
                });

            Assert.True(result.TotalResponses >= 1);
        }


        [Fact]
        public async Task GetCreatorSurveysAsync_NoAutoDeactivation_NoUpdates()
        {
            var service = CreateService("Creator");

            await AddSurvey(
                createdById: 1,
                isActive: true,
                isDeleted: false);

            var result = await service.GetCreatorSurveysAsync(
                1,
                new GetMySurveysRequestDto());

            Assert.Single(result.Surveys);
            Assert.True(result.Surveys.First().IsActive);
        }


        [Fact]
        public async Task GetCreatorSurveysAsync_NotLockedSurvey_IsUnlocked()
        {
            var service = CreateService("Creator");

            var survey = await AddSurvey(
                createdById: 1,
                isActive: true);

            var result = await service.GetCreatorSurveysAsync(
                1,
                new GetMySurveysRequestDto());

            Assert.False(result.Surveys.First().IsLocked);
        }





    }
}