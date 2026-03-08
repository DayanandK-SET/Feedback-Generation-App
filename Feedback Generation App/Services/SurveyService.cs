using ClosedXML.Excel;
using Feedback_Generation_App.Contexts;
using Feedback_Generation_App.Exceptions;
using Feedback_Generation_App.Interfaces;
using Feedback_Generation_App.Models;
using Feedback_Generation_App.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Feedback_Generation_App.Services
{
    public class SurveyService : ISurveyService
    {
        private readonly FeedbackContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SurveyService(
            FeedbackContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        private bool IsAdmin()
        {
            return _httpContextAccessor.HttpContext?.User
                .IsInRole("Admin") ?? false;
        }

        public async Task<string> CreateSurvey(CreateSurveyDto dto, int creatorId)
        {
            var survey = new Survey
            {
                Title = dto.Title,
                Description = dto.Description,
                CreatedById = creatorId,
                IsActive = true,
                ExpireAt = dto.ExpireAt,
                MaxResponses = dto.MaxResponses
            };

            survey.Questions = new List<Question>();

            foreach (var questionDto in dto.Questions)
            {
                Question question;

                if (questionDto.QuestionBankId.HasValue)
                {
                    var bankQuestion = await _context.QuestionBanks
                        .Include(q => q.Options)
                        .FirstOrDefaultAsync(q =>
                            q.Id == questionDto.QuestionBankId.Value &&
                            !q.IsDeleted);

                    if (bankQuestion == null)
                        throw new BadRequestException("QuestionBank not found");

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
                else
                {
                    if (string.IsNullOrWhiteSpace(questionDto.Text) ||
                        !questionDto.QuestionType.HasValue)
                        throw new BadRequestException("Invalid question");

                    question = new Question
                    {
                        Text = questionDto.Text,
                        QuestionType = questionDto.QuestionType.Value,
                        Options = questionDto.Options?
                            .Select(o => new QuestionOption
                            {
                                OptionText = o
                            }).ToList()
                    };
                }

                survey.Questions.Add(question);
            }

            _context.Surveys.Add(survey);
            await _context.SaveChangesAsync();

            return survey.PublicIdentifier;
        }

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

            if (!IsAdmin() && survey.CreatedById != userId)
                throw new ForbiddenException("You do not own this survey");

            if (request.PageNumber < 1) request.PageNumber = 1;
            if (request.PageSize < 1) request.PageSize = 10;

            var responsesQuery = survey.Responses?
                .Where(r => !r.IsDeleted)
                .AsQueryable() ?? new List<Response>().AsQueryable();

            // ✅ DATE FILTER (Now works for Admin too)
            if (request.FromDate.HasValue)
                responsesQuery = responsesQuery
                    .Where(r => r.CreatedAt >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                responsesQuery = responsesQuery
                    .Where(r => r.CreatedAt <= request.ToDate.Value);

            responsesQuery = responsesQuery
                .OrderByDescending(r => r.CreatedAt);

            var totalCount = responsesQuery.Count();

            var paginated = responsesQuery
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new SurveyResponsesDto
            {
                SurveyId = survey.Id,
                Title = survey.Title,
                TotalResponses = totalCount,
                Responses = paginated.Select(r => new ResponseDto
                {
                    ResponseId = r.Id,
                    SubmittedAt = r.CreatedAt,
                    Answers = r.Answers?.Select(a => new AnswerDto
                    {
                        QuestionId = a.QuestionId,
                        QuestionText = a.Question?.Text ?? "",
                        Answer = a.SelectedOption != null
                            ? a.SelectedOption.OptionText
                            : a.AnswerText ?? ""
                    }).ToList()
                }).ToList()
            };
        }

        public async Task DeleteSurveyAsync(int surveyId, int userId)
        {
            var survey = await _context.Surveys
                .FirstOrDefaultAsync(s => s.Id == surveyId && !s.IsDeleted);

            if (survey == null)
                throw new NotFoundException("Survey not found");

            if (!IsAdmin() && survey.CreatedById != userId)
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

            if (!IsAdmin() && survey.CreatedById != userId)
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

            if (!IsAdmin() && survey.CreatedById != userId)
                throw new ForbiddenException("You do not own this survey");

            survey.Title = dto.Title;
            survey.Description = dto.Description;
            survey.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<List<CreatorSurveyListDto>> GetCreatorSurveysAsync(int userId)
        {
            var isAdmin = IsAdmin();

            var surveys = await _context.Surveys
                .Where(s => !s.IsDeleted &&
                           (isAdmin || s.CreatedById == userId))
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
            try
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

                if (!IsAdmin() && survey.CreatedById != userId)
                    throw new ForbiddenException("You do not own this survey");

                var analytics = new SurveyAnalyticsDto
                {
                    SurveyId = survey.Id,
                    Title = survey.Title,
                    TotalResponses = survey.Responses?.Count ?? 0
                };

                foreach (var question in survey.Questions ?? new List<Question>())
                {
                    var questionDto = new QuestionAnalyticsDto
                    {
                        QuestionId = question.Id,
                        QuestionText = question.Text,
                        QuestionType = question.QuestionType
                    };

                    var answers = question.Answers?
                        .Where(a => !a.IsDeleted)
                        .ToList() ?? new List<Answer>();

                    if (question.QuestionType == QuestionType.MultipleChoice)
                    {
                        questionDto.Options = question.Options?
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
                            .Select(a =>
                            {
                                int value;
                                return int.TryParse(a.AnswerText, out value) ? (int?)value : null;
                            })
                            .Where(v => v.HasValue)
                            .Select(v => v!.Value)
                            .ToList();

                        if (ratings.Any())
                        {
                            questionDto.AverageRating = Math.Round(ratings.Average(), 2);
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
            catch (Exception ex)
            {
                throw new ArgumentException("Analytics error: " + ex.Message);
            }
        }


        public async Task<byte[]> ExportResponsesToExcelAsync(int surveyId, int userId)
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

            if (!IsAdmin() && survey.CreatedById != userId)
                throw new ForbiddenException("You do not own this survey");

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Responses");

            worksheet.Cell(1, 1).Value = "ResponseId";
            worksheet.Cell(1, 2).Value = "SubmittedAt";
            worksheet.Cell(1, 3).Value = "Question";
            worksheet.Cell(1, 4).Value = "Answer";

            int row = 2;

            foreach (var response in survey.Responses!)
            {
                foreach (var answer in response.Answers!)
                {
                    worksheet.Cell(row, 1).Value = response.Id;
                    worksheet.Cell(row, 2).Value = response.CreatedAt;
                    worksheet.Cell(row, 3).Value = answer.Question?.Text;

                    var answerText = answer.SelectedOption != null
                        ? answer.SelectedOption.OptionText
                        : answer.AnswerText;

                    worksheet.Cell(row, 4).Value = answerText;

                    row++;
                }
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return stream.ToArray();
        }


        public async Task<string> ImportSurveyFromExcelAsync(
            ImportSurveyExcelDto dto,
            int creatorId)
        {
            if (dto.File == null || dto.File.Length == 0)
                throw new BadRequestException("Excel file is required");

            var survey = new Survey
            {
                Title = dto.Title,
                Description = dto.Description,
                CreatedById = creatorId,
                IsActive = true,
                Questions = new List<Question>()
            };

            using var stream = new MemoryStream();
            await dto.File.CopyToAsync(stream);

            // IMPORTANT: Reset stream position
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);

            var rows = worksheet.RowsUsed().Skip(1);

            foreach (var row in rows)
            {
                var questionText = row.Cell(1).GetString();
                var questionTypeString = row.Cell(2).GetString();

                if (string.IsNullOrWhiteSpace(questionText))
                    continue;

                // Normalize question type (handles "Multiple Choice", spaces, etc.)
                var normalizedType = questionTypeString
                                        .Replace(" ", "")
                                        .Trim();

                if (!Enum.TryParse<QuestionType>(
                        normalizedType,
                        true,
                        out var questionType))
                {
                    throw new BadRequestException(
                        $"Invalid QuestionType: {questionTypeString}");
                }

                var question = new Question
                {
                    Text = questionText,
                    QuestionType = questionType,
                    Options = new List<QuestionOption>()
                };

                // Handle MultipleChoice options dynamically
                if (questionType == QuestionType.MultipleChoice)
                {
                    int lastColumn = row.LastCellUsed().Address.ColumnNumber;

                    for (int i = 3; i <= lastColumn; i++)
                    {
                        var option = row.Cell(i).GetString();

                        if (!string.IsNullOrWhiteSpace(option))
                        {
                            question.Options.Add(new QuestionOption
                            {
                                OptionText = option
                            });
                        }
                    }

                    // Ensure MultipleChoice has options
                    if (!question.Options.Any())
                    {
                        throw new BadRequestException(
                            $"MultipleChoice question '{questionText}' must have options.");
                    }
                }

                survey.Questions.Add(question);
            }

            _context.Surveys.Add(survey);
            await _context.SaveChangesAsync();

            return survey.PublicIdentifier;
        }


        public async Task<List<ResponseTrendDto>> GetSurveyResponseTrendAsync(int surveyId, int userId)
        {
            var survey = await _context.Surveys
                .FirstOrDefaultAsync(s => s.Id == surveyId && !s.IsDeleted);

            if (survey == null)
                throw new NotFoundException("Survey not found");

            if (!IsAdmin() && survey.CreatedById != userId)
                throw new ForbiddenException("You do not own this survey");

            var trend = await _context.Responses
                .Where(r => r.SurveyId == surveyId && !r.IsDeleted)
                .Select(r => new
                {
                    Date = r.CreatedAt.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            var result = trend
                .GroupBy(x => x.Date)
                .Select(g => new ResponseTrendDto
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();

            return result;
        }
    }
}