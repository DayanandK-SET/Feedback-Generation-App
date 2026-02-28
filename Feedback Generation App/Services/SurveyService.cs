using Feedback_Generation_App.Contexts;
using Feedback_Generation_App.Exceptions;
using Feedback_Generation_App.Interfaces;
using Feedback_Generation_App.Models;
using Feedback_Generation_App.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Feedback_Generation_App.Services
{
    public class SurveyService : ISurveyService
    {
        private readonly FeedbackContext _context;

        public SurveyService(FeedbackContext context)
        {
            _context = context;
        }

        public async Task<string> CreateSurvey(CreateSurveyDto dto, int creatorId)
        {
            var survey = new Survey
            {
                Title = dto.Title,
                Description = dto.Description,
                CreatedById = creatorId,
                IsActive = true
            };

            survey.Questions = new List<Question>();

            foreach (var questionDto in dto.Questions)
            {
                Question question;

                // 🔹 CASE 1: Using QuestionBank
                if (questionDto.QuestionBankId.HasValue)
                {
                    var bankQuestion = await _context.QuestionBanks
                        .Include(q => q.Options)
                        .FirstOrDefaultAsync(q =>
                            q.Id == questionDto.QuestionBankId.Value &&
                            q.CreatedById == creatorId &&
                            !q.IsDeleted);

                    if (bankQuestion == null)
                        throw new BadRequestException($"QuestionBank with Id {questionDto.QuestionBankId} not found");

                    question = new Question
                    {
                        Text = bankQuestion.Text,
                        QuestionType = bankQuestion.QuestionType,
                        Options = bankQuestion.Options?
                            .Select(o => new QuestionOption
                            {
                                OptionText = o.OptionText
                            }).ToList()
                    };
                }
                // 🔹 CASE 2: Manual question (Old functionality)
                else
                {
                    if (string.IsNullOrWhiteSpace(questionDto.Text) ||
                        !questionDto.QuestionType.HasValue)
                    {
                        throw new BadRequestException("Text and QuestionType are required when not using QuestionBankId");
                    }

                    question = new Question
                    {
                        Text = questionDto.Text,
                        QuestionType = questionDto.QuestionType.Value
                    };

                    if (questionDto.Options != null && questionDto.Options.Any())
                    {
                        question.Options = questionDto.Options
                            .Select(o => new QuestionOption
                            {
                                OptionText = o
                            }).ToList();
                    }
                }

                survey.Questions.Add(question);
            }

            _context.Surveys.Add(survey);
            await _context.SaveChangesAsync();

            return survey.PublicIdentifier;
        }

        // ✅ PAGINATION ADDED HERE
        public async Task<SurveyResponsesDto> GetSurveyResponsesAsync(
            int surveyId,
            int userId,
            GetSurveyResponsesRequestDto request)
        {
            var survey = await _context.Surveys
                .Include(s => s.Responses)
                    .ThenInclude(r => r.Answers)
                        .ThenInclude(a => a.Question)
                .Include(s => s.Responses)
                    .ThenInclude(r => r.Answers)
                        .ThenInclude(a => a.SelectedOption)
                .FirstOrDefaultAsync(s => s.Id == surveyId && !s.IsDeleted);

            if (survey == null)
                throw new NotFoundException("Survey not found");

            if (survey.CreatedById != userId)
                throw new ForbiddenException("You do not own this survey");

            // Safety defaults
            if (request.PageNumber < 1)
                request.PageNumber = 1;

            if (request.PageSize < 1)
                request.PageSize = 10;

            var allResponses = survey.Responses?
                .Where(r => !r.IsDeleted)
                .OrderByDescending(r => r.CreatedAt)
                .ToList() ?? new List<Response>();

            // ✅ Apply QuestionType filter if provided
            if (request.QuestionType.HasValue)
            {
                foreach (var response in allResponses)
                {
                    response.Answers = response.Answers?
                        .Where(a => a.Question != null &&
                                    a.Question.QuestionType == request.QuestionType.Value)
                        .ToList();
                }

                // Remove responses that no longer have answers after filtering
                allResponses = allResponses
                    .Where(r => r.Answers != null && r.Answers.Any())
                    .ToList();
            }

            var paginatedResponses = allResponses
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var result = new SurveyResponsesDto
            {
                SurveyId = survey.Id,
                Title = survey.Title,
                TotalResponses = allResponses.Count, // FULL count (not paginated)
                Responses = paginatedResponses.Select(r => new ResponseDto
                {
                    ResponseId = r.Id,
                    SubmittedAt = r.CreatedAt,
                    Answers = r.Answers?.Select(a => new AnswerDto
                    {
                        QuestionId = a.QuestionId,
                        QuestionText = a.Question?.Text ?? string.Empty,
                        Answer = a.SelectedOption != null
                            ? a.SelectedOption.OptionText
                            : a.AnswerText ?? string.Empty
                    }).ToList()
                }).ToList()
            };

            return result;
        }

        public async Task DeleteSurveyAsync(int surveyId, int userId)
        {
            var survey = await _context.Surveys
                .FirstOrDefaultAsync(s => s.Id == surveyId && !s.IsDeleted);

            if (survey == null)
                throw new NotFoundException("Survey not found");

            if (survey.CreatedById != userId)
                throw new ForbiddenException("You do not own this survey");

            survey.IsDeleted = true;

            await _context.SaveChangesAsync();
        }

        public async Task ToggleSurveyStatusAsync(int surveyId, int userId)
        {
            var survey = await _context.Surveys
                .FirstOrDefaultAsync(s => s.Id == surveyId && !s.IsDeleted);

            if (survey == null)
                throw new NotFoundException("Survey not found");

            if (survey.CreatedById != userId)
                throw new ForbiddenException("You do not own this survey");

            survey.IsActive = !survey.IsActive;

            await _context.SaveChangesAsync();
        }

        public async Task UpdateSurveyAsync(int surveyId, int userId, UpdateSurveyDto dto)
        {
            var survey = await _context.Surveys
                .FirstOrDefaultAsync(s => s.Id == surveyId && !s.IsDeleted);

            if (survey == null)
                throw new NotFoundException("Survey not found");

            if (survey.CreatedById != userId)
                throw new ForbiddenException("You do not own this survey");

            survey.Title = dto.Title;
            survey.Description = dto.Description;
            survey.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<List<CreatorSurveyListDto>> GetCreatorSurveysAsync(int userId)
        {
            var surveys = await _context.Surveys
                .Where(s => s.CreatedById == userId && !s.IsDeleted)
                .Include(s => s.Responses)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new CreatorSurveyListDto
                {
                    SurveyId = s.Id,
                    Title = s.Title,
                    Description = s.Description,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt,
                    TotalResponses = s.Responses.Count,
                    PublicIdentifier = s.PublicIdentifier
                })
                .ToListAsync();

            return surveys;
        }

        public async Task<SurveyAnalyticsDto> GetSurveyAnalyticsAsync(int surveyId, int userId)
        {
            var survey = await _context.Surveys
                .Include(s => s.Responses)
                .Include(s => s.Questions)
                    .ThenInclude(q => q.Options)
                .Include(s => s.Questions)
                    .ThenInclude(q => q.Answers)
                        .ThenInclude(a => a.SelectedOption)
                .FirstOrDefaultAsync(s => s.Id == surveyId && !s.IsDeleted);

            if (survey == null)
                throw new NotFoundException("Survey not found");

            if (survey.CreatedById != userId)
                throw new ForbiddenException("You do not own this survey");

            var analytics = new SurveyAnalyticsDto
            {
                SurveyId = survey.Id,
                Title = survey.Title,
                TotalResponses = survey.Responses?.Count ?? 0
            };

            foreach (var question in survey.Questions!)
            {
                var questionDto = new QuestionAnalyticsDto
                {
                    QuestionId = question.Id,
                    QuestionText = question.Text,
                    QuestionType = question.QuestionType
                };

                var answers = question.Answers?.Where(a => !a.IsDeleted).ToList() ?? new List<Answer>();

                if (question.QuestionType == QuestionType.MultipleChoice)
                {
                    questionDto.Options = question.Options!
                        .Select(o => new OptionAnalyticsDto
                        {
                            OptionText = o.OptionText,
                            Count = answers.Count(a => a.SelectedOptionId == o.Id)
                        })
                        .ToList();
                }
                else if (question.QuestionType == QuestionType.Rating)
                {
                    var ratings = answers
                        .Where(a => !string.IsNullOrEmpty(a.AnswerText))
                        .Select(a => int.Parse(a.AnswerText!))
                        .ToList();

                    if (ratings.Any())
                    {
                        questionDto.AverageRating = ratings.Average();
                        questionDto.MinRating = ratings.Min();
                        questionDto.MaxRating = ratings.Max();
                    }
                }
                else if (question.QuestionType == QuestionType.Text)
                {
                    questionDto.TextAnswers = answers
                        .Where(a => !string.IsNullOrEmpty(a.AnswerText))
                        .Select(a => a.AnswerText!)
                        .ToList();
                }

                analytics.Questions.Add(questionDto);
            }

            return analytics;
        }


    }
}