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
    // Tests: CreateQuestionsAsync, GetMyQuestionsAsync


    public class QuestionBankServiceTests
    {
        private readonly FeedbackContext _context;
        private readonly IRepository<int, QuestionBank> _questionBankRepository;
        private readonly QuestionBankService _questionBankService;

        public QuestionBankServiceTests()
        {
            var options = new DbContextOptionsBuilder<FeedbackContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new FeedbackContext(options);
            _questionBankRepository = new Repository<int, QuestionBank>(_context);
            _questionBankService = new QuestionBankService(_questionBankRepository);
        }

        //  Helper 
        private List<CreateQuestionBankDto> MakeDtos(params (string text, QuestionType type)[] items)
        {
            return items.Select(i => new CreateQuestionBankDto
            {
                Text = i.text,
                QuestionType = i.type
            }).ToList();
        }


        // CreateQuestionsAsync Tests

        [Fact]
        public async Task CreateQuestionsAsync_SingleTextQuestion_ReturnsOneId()
        {
            // Arrange
            var dtos = MakeDtos(("How was your experience?", QuestionType.Text));

            // Act
            var ids = await _questionBankService.CreateQuestionsAsync(dtos, userId: 1);

            // Assert
            Assert.NotNull(ids);
            Assert.Single(ids);
            Assert.True(ids[0] > 0);
        }

        [Fact]
        public async Task CreateQuestionsAsync_SingleRatingQuestion_IsSavedWithCorrectType()
        {
            // Arrange
            var dtos = MakeDtos(("Rate our service", QuestionType.Rating));

            // Act
            var ids = await _questionBankService.CreateQuestionsAsync(dtos, userId: 1);

            // Assert
            var saved = await _questionBankRepository.GetQueryable()
                .FirstAsync(q => q.Id == ids[0]);

            Assert.Equal(QuestionType.Rating, saved.QuestionType);
        }

        [Fact]
        public async Task CreateQuestionsAsync_MultipleChoiceWithOptions_OptionsAreSaved()
        {
            // Arrange
            var dtos = new List<CreateQuestionBankDto>
            {
                new CreateQuestionBankDto
                {
                    Text = "Would you recommend us?",
                    QuestionType = QuestionType.MultipleChoice,
                    Options = new List<string> { "Yes", "No", "Maybe" }
                }
            };

            // Act
            var ids = await _questionBankService.CreateQuestionsAsync(dtos, userId: 1);

            // Assert
            var saved = await _questionBankRepository.GetQueryable()
                .Include(q => q.Options)
                .FirstAsync(q => q.Id == ids[0]);

            Assert.NotNull(saved.Options);
            Assert.Equal(3, saved.Options!.Count);
            Assert.Contains(saved.Options, o => o.OptionText == "Yes");
            Assert.Contains(saved.Options, o => o.OptionText == "No");
            Assert.Contains(saved.Options, o => o.OptionText == "Maybe");
        }

        [Fact]
        public async Task CreateQuestionsAsync_MultipleQuestions_ReturnsAllIds()
        {
            // Arrange
            var dtos = new List<CreateQuestionBankDto>
            {
                new CreateQuestionBankDto { Text = "Q1", QuestionType = QuestionType.Text },
                new CreateQuestionBankDto { Text = "Q2", QuestionType = QuestionType.Rating },
                new CreateQuestionBankDto
                {
                    Text = "Q3",
                    QuestionType = QuestionType.MultipleChoice,
                    Options = new List<string> { "A", "B" }
                }
            };

            // Act
            var ids = await _questionBankService.CreateQuestionsAsync(dtos, userId: 1);

            // Assert
            Assert.Equal(3, ids.Count);
            Assert.All(ids, id => Assert.True(id > 0));

            // All IDs must be unique
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }

        [Fact]
        public async Task CreateQuestionsAsync_QuestionsLinkedToCreator_CreatedByIdIsSet()
        {
            // Arrange
            var dtos = MakeDtos(("My question", QuestionType.Text));

            // Act
            var ids = await _questionBankService.CreateQuestionsAsync(dtos, userId: 42);

            // Assert
            var saved = await _questionBankRepository.GetQueryable()
                .FirstAsync(q => q.Id == ids[0]);

            Assert.Equal(42, saved.CreatedById);
        }

        [Fact]
        public async Task CreateQuestionsAsync_EmptyList_ThrowsBadRequestException()
        {
            // Arrange
            var dtos = new List<CreateQuestionBankDto>();

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(async () =>
                await _questionBankService.CreateQuestionsAsync(dtos, userId: 1)
            );
        }

        [Fact]
        public async Task CreateQuestionsAsync_NullList_ThrowsBadRequestException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(async () =>
                await _questionBankService.CreateQuestionsAsync(null!, userId: 1)
            );
        }

        [Fact]
        public async Task CreateQuestionsAsync_BlankQuestionText_ThrowsBadRequestException()
        {
            // Arrange
            var dtos = new List<CreateQuestionBankDto>
            {
                new CreateQuestionBankDto
                {
                    Text = "   ",   // whitespace only
                    QuestionType = QuestionType.Text
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(async () =>
                await _questionBankService.CreateQuestionsAsync(dtos, userId: 1)
            );
        }


        // GetMyQuestionsAsync Tests

        [Fact]
        public async Task GetMyQuestionsAsync_CreatorUser_ReturnsOnlyOwnQuestions()
        {
            // Arrange — creator 1 adds 2 questions, creator 2 adds 1
            await _questionBankService.CreateQuestionsAsync(
                MakeDtos(("C1 Q1", QuestionType.Text), ("C1 Q2", QuestionType.Rating)),
                userId: 1);

            await _questionBankService.CreateQuestionsAsync(
                MakeDtos(("C2 Q1", QuestionType.Text)),
                userId: 2);

            var request = new GetQuestionBankRequestDto { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _questionBankService.GetMyQuestionsAsync(
                userId: 1, isAdmin: false, request);

            // Assert — creator 1 sees only their 2 questions
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Questions, q => Assert.StartsWith("C1", q.Text));
        }

        [Fact]
        public async Task GetMyQuestionsAsync_AdminUser_ReturnsAllCreatorsQuestions()
        {
            // Arrange
            await _questionBankService.CreateQuestionsAsync(
                MakeDtos(("C1 Q1", QuestionType.Text)), userId: 1);

            await _questionBankService.CreateQuestionsAsync(
                MakeDtos(("C2 Q1", QuestionType.Text)), userId: 2);

            var request = new GetQuestionBankRequestDto { PageNumber = 1, PageSize = 10 };

            // Act — admin sees everything
            var result = await _questionBankService.GetMyQuestionsAsync(
                userId: 1, isAdmin: true, request);

            // Assert
            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public async Task GetMyQuestionsAsync_TypeFilter_ReturnsOnlyMatchingType()
        {
            // Arrange — add all 3 types
            await _questionBankService.CreateQuestionsAsync(
                new List<CreateQuestionBankDto>
                {
                    new CreateQuestionBankDto { Text = "Text Q", QuestionType = QuestionType.Text },
                    new CreateQuestionBankDto { Text = "Rating Q", QuestionType = QuestionType.Rating },
                    new CreateQuestionBankDto
                    {
                        Text = "MC Q",
                        QuestionType = QuestionType.MultipleChoice,
                        Options = new List<string> { "A", "B" }
                    }
                },
                userId: 1);

            var request = new GetQuestionBankRequestDto
            {
                PageNumber = 1,
                PageSize = 10,
                QuestionType = QuestionType.Rating
            };

            // Act
            var result = await _questionBankService.GetMyQuestionsAsync(
                userId: 1, isAdmin: false, request);

            // Assert — only 1 Rating question
            Assert.Equal(1, result.TotalCount);
            Assert.Equal(QuestionType.Rating, result.Questions[0].QuestionType);
        }

        [Fact]
        public async Task GetMyQuestionsAsync_Pagination_ReturnsCorrectPageAndCount()
        {
            // Arrange — add 5 questions
            await _questionBankService.CreateQuestionsAsync(
                Enumerable.Range(1, 5)
                    .Select(i => new CreateQuestionBankDto
                    {
                        Text = $"Question {i}",
                        QuestionType = QuestionType.Text
                    }).ToList(),
                userId: 1);

            var request = new GetQuestionBankRequestDto
            {
                PageNumber = 2,
                PageSize = 2
            };

            // Act
            var result = await _questionBankService.GetMyQuestionsAsync(
                userId: 1, isAdmin: false, request);

            // Assert
            Assert.Equal(5, result.TotalCount);     // total across all pages
            Assert.Equal(2, result.Questions.Count); // only 2 on page 2
            Assert.Equal(2, result.PageNumber);
        }

        [Fact]
        public async Task GetMyQuestionsAsync_NoQuestions_ReturnsEmptyResult()
        {
            // Arrange — no questions added
            var request = new GetQuestionBankRequestDto { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _questionBankService.GetMyQuestionsAsync(
                userId: 99, isAdmin: false, request);

            // Assert
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Questions);
        }
    }
}
