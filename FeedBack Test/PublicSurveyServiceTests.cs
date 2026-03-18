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

    // Tests: GetSurvey, SubmitSurvey
 

    public class PublicSurveyServiceTests
    {
        private readonly FeedbackContext _context;
        private readonly IRepository<int, Survey> _surveyRepository;
        private readonly IRepository<int, Response> _responseRepository;
        private readonly PublicSurveyService _publicSurveyService;

        public PublicSurveyServiceTests()
        {
            var options = new DbContextOptionsBuilder<FeedbackContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new FeedbackContext(options);
            _surveyRepository = new Repository<int, Survey>(_context);
            _responseRepository = new Repository<int, Response>(_context);

            _publicSurveyService = new PublicSurveyService(
                _surveyRepository,
                _responseRepository
            );
        }

        //  Helpers 

        private async Task<Survey> CreateActiveSurvey(
            string identifier = "test-id-001",
            bool isActive = true,
            DateTime? expireAt = null,
            int? maxResponses = null)
        {
            var survey = new Survey
            {
                Title = "Test Survey",
                Description = "Test Description",
                PublicIdentifier = identifier,
                IsActive = isActive,
                CreatedById = 1,
                ExpireAt = expireAt,
                MaxResponses = maxResponses,
                Questions = new List<Question>
                {
                    new Question
                    {
                        Text = "How was your experience?",
                        QuestionType = QuestionType.Text
                    },
                    new Question
                    {
                        Text = "Rate us",
                        QuestionType = QuestionType.Rating
                    }
                }
            };

            await _surveyRepository.AddAsync(survey);
            return survey;
        }

        private SubmitSurveyDto MakeValidSubmit(Survey survey, string token = "unique-token-001")
        {
            var questions = _context.Set<Question>()
                .Where(q => q.SurveyId == survey.Id)
                .ToList();

            return new SubmitSurveyDto
            {
                ResponseToken = token,
                Answers = questions.Select(q => new SubmitAnswerDto
                {
                    QuestionId = q.Id,
                    TextAnswer = q.QuestionType == QuestionType.Text ? "Great!" : null,
                    RatingValue = q.QuestionType == QuestionType.Rating ? 8 : null
                }).ToList()
            };
        }


        // GetSurvey Tests

        [Fact]
        public async Task GetSurvey_ActiveSurvey_ReturnsSurveyDto()
        {
            // Arrange
            var survey = await CreateActiveSurvey("active-001");

            // Act
            var result = await _publicSurveyService.GetSurvey("active-001");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Survey", result.Title);
            Assert.Equal("Test Description", result.Description);
            Assert.Equal(2, result.Questions.Count);
        }

        [Fact]
        public async Task GetSurvey_InactiveSurvey_ReturnsNull()
        {
            // Arrange
            await CreateActiveSurvey("inactive-001", isActive: false);

            // Act
            var result = await _publicSurveyService.GetSurvey("inactive-001");

            // Assert — inactive surveys should not be accessible publicly
            Assert.Null(result);
        }

        [Fact]
        public async Task GetSurvey_NonExistentIdentifier_ReturnsNull()
        {
            // Act
            var result = await _publicSurveyService.GetSurvey("does-not-exist");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetSurvey_SurveyWithExpiredDate_ThrowsBadRequestException()
        {
            // Arrange — survey expired yesterday
            await CreateActiveSurvey(
                "expired-001",
                expireAt: DateTime.UtcNow.AddDays(-1));

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(async () =>
                await _publicSurveyService.GetSurvey("expired-001")
            );
        }

        [Fact]
        public async Task GetSurvey_SurveyExpiringInFuture_ReturnsSurveyDto()
        {
            // Arrange — survey expires tomorrow (still valid)
            await CreateActiveSurvey(
                "future-expire-001",
                expireAt: DateTime.UtcNow.AddDays(1));

            // Act
            var result = await _publicSurveyService.GetSurvey("future-expire-001");

            // Assert — still active
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetSurvey_QuestionsIncludedInResponse()
        {
            // Arrange
            await CreateActiveSurvey("with-questions-001");

            // Act
            var result = await _publicSurveyService.GetSurvey("with-questions-001");

            // Assert — questions must be returned with their IDs
            Assert.NotNull(result);
            Assert.All(result.Questions, q => Assert.True(q.QuestionId > 0));
            Assert.All(result.Questions, q => Assert.NotEmpty(q.Text));
        }


        // SubmitSurvey Tests

        [Fact]
        public async Task SubmitSurvey_ValidAnswers_ResponseSavedToDatabase()
        {
            // Arrange
            var survey = await CreateActiveSurvey("submit-001");
            var dto = MakeValidSubmit(survey, "token-submit-001");

            // Act
            await _publicSurveyService.SubmitSurvey("submit-001", dto);

            // Assert — one response saved
            var saved = await _responseRepository.GetQueryable()
                .FirstOrDefaultAsync(r =>
                    r.SurveyId == survey.Id &&
                    r.ResponseToken == "token-submit-001");

            Assert.NotNull(saved);
        }

        [Fact]
        public async Task SubmitSurvey_DuplicateToken_ThrowsArgumentException()
        {
            // Arrange
            var survey = await CreateActiveSurvey("duplicate-001");
            var dto = MakeValidSubmit(survey, "same-token");

            // First submission
            await _publicSurveyService.SubmitSurvey("duplicate-001", dto);

            // Act & Assert — second submission with same token
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _publicSurveyService.SubmitSurvey("duplicate-001", dto)
            );
        }

        [Fact]
        public async Task SubmitSurvey_EmptyResponseToken_ThrowsArgumentException()
        {
            // Arrange
            var survey = await CreateActiveSurvey("empty-token-001");
            var dto = MakeValidSubmit(survey, token: "");
            dto.ResponseToken = "";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _publicSurveyService.SubmitSurvey("empty-token-001", dto)
            );
        }

        [Fact]
        public async Task SubmitSurvey_InactiveSurvey_ThrowsBadRequestException()
        {
            // Arrange
            await CreateActiveSurvey("inactive-submit-001", isActive: false);

            var dto = new SubmitSurveyDto
            {
                ResponseToken = "token-999",
                Answers = new List<SubmitAnswerDto>()
            };

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(async () =>
                await _publicSurveyService.SubmitSurvey("inactive-submit-001", dto)
            );
        }

        [Fact]
        public async Task SubmitSurvey_ExpiredSurvey_ThrowsBadRequestException()
        {
            // Arrange — expired yesterday
            await CreateActiveSurvey(
                "expired-submit-001",
                expireAt: DateTime.UtcNow.AddDays(-1));

            var dto = new SubmitSurveyDto
            {
                ResponseToken = "token-expired",
                Answers = new List<SubmitAnswerDto>()
            };

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(async () =>
                await _publicSurveyService.SubmitSurvey("expired-submit-001", dto)
            );
        }

        [Fact]
        public async Task SubmitSurvey_MaxResponsesReached_ThrowsBadRequestException()
        {
            // Arrange — max 1 response allowed
            var survey = await CreateActiveSurvey("maxresp-001", maxResponses: 1);

            // First submission fills the limit
            var dto1 = MakeValidSubmit(survey, "first-token");
            await _publicSurveyService.SubmitSurvey("maxresp-001", dto1);

            // Act & Assert — second submission exceeds limit
            var dto2 = MakeValidSubmit(survey, "second-token");
            await Assert.ThrowsAsync<BadRequestException>(async () =>
                await _publicSurveyService.SubmitSurvey("maxresp-001", dto2)
            );
        }

        [Fact]
        public async Task SubmitSurvey_DifferentTokens_BothSubmissionsAccepted()
        {
            // Arrange — max responses = 2
            var survey = await CreateActiveSurvey("multi-token-001", maxResponses: 2);

            var dto1 = MakeValidSubmit(survey, "token-A");
            var dto2 = MakeValidSubmit(survey, "token-B");

            // Act
            await _publicSurveyService.SubmitSurvey("multi-token-001", dto1);
            await _publicSurveyService.SubmitSurvey("multi-token-001", dto2);

            // Assert — 2 responses saved
            var count = await _responseRepository.GetQueryable()
                .CountAsync(r => r.SurveyId == survey.Id && !r.IsDeleted);

            Assert.Equal(2, count);
        }
    }
}
