using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.InkML;
using Feedback_Generation_App.Contexts;
using Feedback_Generation_App.Exceptions;
using Feedback_Generation_App.Interfaces;
using Feedback_Generation_App.Models;
using Feedback_Generation_App.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Feedback_Generation_App.Services
{
    public class SurveyService : ISurveyService
    {
        private readonly IRepository<int, Survey> _surveyRepository;
        private readonly IRepository<int, QuestionBank> _bankRepository;
        private readonly IRepository<int, Response> _responsesRepository;
        private readonly IRepository<int, AuditLog> _auditLogRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IRepository<int, User> _userRepository;



        public SurveyService(
    IHttpContextAccessor httpContextAccessor,
    IRepository<int, Survey> surveyRepository,
    IRepository<int, QuestionBank> bankRepository,
    IRepository<int, Response> responsesRepository,
    IRepository<int, AuditLog> auditLogRepository,
    IRepository<int, User> userRepository)   
        {
            _httpContextAccessor = httpContextAccessor;
            _surveyRepository = surveyRepository;
            _bankRepository = bankRepository;
            _responsesRepository = responsesRepository;
            _auditLogRepository = auditLogRepository;
            _userRepository = userRepository;
        }



        private string GetCurrentUsername()
        {
            return _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        }

        private bool IsAdmin()
        {
            return _httpContextAccessor.HttpContext?.User
                .IsInRole("Admin") ?? false;
        }

        public async Task<string> CreateSurvey(CreateSurveyDto dto, int creatorId)
        {

            // ✅ NEW: Block inactive creators from creating surveys
            var creator = await _userRepository.GetQueryable()
                .FirstOrDefaultAsync(u => u.Id == creatorId);

            if (creator == null || creator.IsDeleted)
                throw new ForbiddenException(
                    "Your account has been deactivated by the admin. You cannot create surveys.");





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
                    //var bankQuestion = await _context.QuestionBanks
                    var bankQuestion = await _bankRepository.GetQueryable()
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

            //_context.Surveys.Add(survey);
            await _surveyRepository.AddAsync(survey);
            //await _context.SaveChangesAsync();

            return survey.PublicIdentifier;
        }

        public async Task<SurveyResponsesDto> GetSurveyResponsesAsync(
            int surveyId,
            int userId,
            GetSurveyResponsesRequestDto request)
        {
            //var survey = await _context.Surveys
            var survey = await _surveyRepository.GetQueryable()
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


            // The below line is for making Queryable

            var responsesQuery = survey.Responses?
                .Where(r => !r.IsDeleted)
                .AsQueryable() ?? new List<Response>().AsQueryable();  //AsQueryable()

            // Date Filter
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
            var survey = await _surveyRepository.GetQueryable()
                .FirstOrDefaultAsync(s => s.Id == surveyId && !s.IsDeleted);

            if (survey == null)
                throw new NotFoundException("Survey not found");

            if (!IsAdmin() && survey.CreatedById != userId)
                throw new ForbiddenException("You do not own this survey");

            survey.IsDeleted = true;

            await _surveyRepository.UpdateAsync(surveyId, survey);

            // write audit log
            await _auditLogRepository.AddAsync(new AuditLog
            {
                Action = "Survey Deleted",
                SurveyId = survey.Id,
                SurveyTitle = survey.Title,
                PerformedBy = GetCurrentUsername(),
                PerformedAt = DateTime.UtcNow
            });
        }






        //public async Task DeleteSurveyAsync(int surveyId, int userId)
        //{
        //    //var survey = await _context.Surveys
        //    var survey = await _surveyRepository.GetQueryable()
        //        .FirstOrDefaultAsync(s => s.Id == surveyId && !s.IsDeleted);

        //    if (survey == null)
        //        throw new NotFoundException("Survey not found");

        //    if (!IsAdmin() && survey.CreatedById != userId)
        //        throw new ForbiddenException("You do not own this survey");

        //    survey.IsDeleted = true;
        //    //await _context.SaveChangesAsync();

        //    await _surveyRepository.UpdateAsync(surveyId, survey);
        //}

        //public async Task ToggleSurveyStatusAsync(int surveyId, int userId)
        //{
        //    //var survey = await _context.Surveys
        //    var survey = await _surveyRepository.GetQueryable()
        //        .FirstOrDefaultAsync(s => s.Id == surveyId && !s.IsDeleted);

        //    if (survey == null)
        //        throw new NotFoundException("Survey not found");

        //    if (!IsAdmin() && survey.CreatedById != userId)
        //        throw new ForbiddenException("You do not own this survey");

        //    survey.IsActive = !survey.IsActive;
        //    //await _context.SaveChangesAsync();

        //    await _surveyRepository.UpdateAsync(survey.Id, survey);


        //}


        public async Task ToggleSurveyStatusAsync(int surveyId, int userId)
        {
            var survey = await _surveyRepository.GetQueryable()
                .FirstOrDefaultAsync(s => s.Id == surveyId && !s.IsDeleted);

            if (survey == null)
                throw new NotFoundException("Survey not found");

            if (!IsAdmin() && survey.CreatedById != userId)
                throw new ForbiddenException("You do not own this survey");

            //if (!survey.IsActive) // means we are trying to ACTIVATE
            //{
            //    // Expiry check
            //    if (survey.ExpireAt.HasValue && survey.ExpireAt.Value < DateTime.UtcNow)
            //        throw new BadRequestException("Cannot activate an expired survey");

            //    // Max responses check
            //    if (survey.MaxResponses.HasValue)
            //    {
            //        var responseCount = survey.Responses?
            //            .Count(r => !r.IsDeleted) ?? 0;

            //        if (responseCount >= survey.MaxResponses.Value)
            //            throw new BadRequestException("Cannot activate survey: max responses reached");
            //    }
            //}

            survey.IsActive = !survey.IsActive;

            await _surveyRepository.UpdateAsync(survey.Id, survey);

            // audit log
            var action = survey.IsActive ? "Survey Activated" : "Survey Deactivated";
            await _auditLogRepository.AddAsync(new AuditLog
            {
                Action = action,
                SurveyId = survey.Id,
                SurveyTitle = survey.Title,
                PerformedBy = GetCurrentUsername(),
                PerformedAt = DateTime.UtcNow
            });
        }


        // UpdateSurvey

        public async Task UpdateSurveyAsync(int surveyId, int userId, UpdateSurveyDto dto)
        {
            var survey = await _surveyRepository.GetQueryable()
                .FirstOrDefaultAsync(s => s.Id == surveyId && !s.IsDeleted);

            if (survey == null)
                throw new NotFoundException("Survey not found");

            if (!IsAdmin() && survey.CreatedById != userId)
                throw new ForbiddenException("You do not own this survey");

            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new BadRequestException("Title is required");

            survey.Title = dto.Title;
            survey.Description = dto.Description;
            survey.ExpireAt = dto.ExpireAt;
            survey.MaxResponses = dto.MaxResponses;
            survey.UpdatedAt = DateTime.UtcNow;

            await _surveyRepository.UpdateAsync(surveyId, survey);

            // audit log
            await _auditLogRepository.AddAsync(new AuditLog
            {
                Action = "Survey Updated",
                SurveyId = survey.Id,
                SurveyTitle = survey.Title,
                PerformedBy = GetCurrentUsername(),
                PerformedAt = DateTime.UtcNow
            });
        }




        //public async Task UpdateSurveyAsync(int surveyId, int userId, UpdateSurveyDto dto)
        //{
        //    //var survey = await _context.Surveys
        //    var survey = await _surveyRepository.GetQueryable()
        //        .FirstOrDefaultAsync(s => s.Id == surveyId && !s.IsDeleted);

        //    if (survey == null)
        //        throw new NotFoundException("Survey not found");

        //    if (!IsAdmin() && survey.CreatedById != userId)
        //        throw new ForbiddenException("You do not own this survey");

        //    if (string.IsNullOrWhiteSpace(dto.Title))
        //        throw new BadRequestException("Title is required");

        //    survey.Title = dto.Title;
        //    survey.Description = dto.Description;
        //    survey.UpdatedAt = DateTime.UtcNow;

        //    //await _context.SaveChangesAsync();
        //    await _surveyRepository.UpdateAsync(surveyId, survey);
        //}


        //    public async Task<PagedSurveyResponseDto> GetCreatorSurveysAsync(
        //int userId, GetMySurveysRequestDto request)
        //    {
        //        // Pagination
        //        if (request.PageNumber < 1) request.PageNumber = 1;
        //        if (request.PageSize < 1) request.PageSize = 10;

        //        var now = DateTime.UtcNow;

        //        // Base query (Only creator's surveys, excluded deleted)
        //        var query = _surveyRepository.GetQueryable()
        //            .Where(s => s.CreatedById == userId && !s.IsDeleted);

        //        if (request.FromDate.HasValue)
        //            query = query.Where(s => s.CreatedAt >= request.FromDate.Value);

        //        if (request.ToDate.HasValue)
        //            query = query.Where(s => s.CreatedAt <= request.ToDate.Value);

        //        if (request.IsActive.HasValue)
        //            query = query.Where(s => s.IsActive == request.IsActive.Value);

        //        // Total surveys count 
        //        var totalCount = await query.CountAsync();


        //        int totalActiveSurveys = await query
        //.CountAsync(s => s.IsActive);

        //        // Total responses count
        //        int totalResponsesCount = await query
        //            .SelectMany(s => s.Responses)
        //            .CountAsync(r => !r.IsDeleted);


        //        // Get paginated surveys with responses
        //        var surveys = await query
        //            .Include(s => s.Responses)
        //            .OrderByDescending(s => s.CreatedAt)
        //            .Skip((request.PageNumber - 1) * request.PageSize)
        //            .Take(request.PageSize)
        //            .ToListAsync();

        //        // Auto-deactivate logic
        //        bool anyChanges = false;

        //        foreach (var survey in surveys)
        //        {
        //            bool shouldDeactivate = false;

        //            // Expiry check
        //            if (survey.ExpireAt.HasValue && survey.ExpireAt.Value < now)
        //                shouldDeactivate = true;

        //            // Max response check
        //            if (survey.MaxResponses.HasValue)
        //            {
        //                var count = survey.Responses?.Count(r => !r.IsDeleted) ?? 0;
        //                if (count >= survey.MaxResponses.Value)
        //                    shouldDeactivate = true;
        //            }

        //            // Apply change only if needed
        //            if (shouldDeactivate && survey.IsActive)
        //            {
        //                survey.IsActive = false;
        //                anyChanges = true;
        //            }
        //        }

        //        // If any changes to DB
        //        if (anyChanges)
        //        {
        //            foreach (var survey in surveys.Where(s => !s.IsActive))
        //            {
        //                await _surveyRepository.UpdateAsync(survey.Id, survey);
        //            }
        //        }


        //        return new PagedSurveyResponseDto
        //        {
        //            TotalCount = totalCount,
        //            PageNumber = request.PageNumber,
        //            PageSize = request.PageSize,
        //            TotalResponsesCount = totalResponsesCount,
        //            TotalActiveSurveys = totalActiveSurveys,


        //            Surveys = surveys.Select(s => new CreatorSurveyListDto
        //            {
        //                SurveyId = s.Id,
        //                Title = s.Title,
        //                Description = s.Description,
        //                IsActive = s.IsActive,
        //                CreatedAt = s.CreatedAt,
        //                TotalResponses = s.Responses?.Count(r => !r.IsDeleted) ?? 0,
        //                PublicIdentifier = s.PublicIdentifier
        //            }).ToList()
        //        };
        //    }



        public async Task<PagedSurveyResponseDto> GetCreatorSurveysAsync(
    int userId, GetMySurveysRequestDto request)
        {
            // Pagination safety
            if (request.PageNumber < 1) request.PageNumber = 1;
            if (request.PageSize < 1) request.PageSize = 10;

            var now = DateTime.UtcNow;

            // Base query (creator only, not deleted)
            var baseQuery = _surveyRepository.GetQueryable()
                .Where(s => s.CreatedById == userId && !s.IsDeleted)
                .Include(s => s.Responses);

            // Auto-deactivate 
            var allSurveys = await baseQuery.ToListAsync();

            bool anyChanges = false;

            foreach (var survey in allSurveys)
            {
                bool shouldDeactivate = false;

                // Expiry check
                if (survey.ExpireAt.HasValue && survey.ExpireAt.Value < now)
                    shouldDeactivate = true;

                // Max response check
                if (survey.MaxResponses.HasValue)
                {
                    var count = survey.Responses?.Count(r => !r.IsDeleted) ?? 0;
                    if (count >= survey.MaxResponses.Value)
                        shouldDeactivate = true;
                }

                if (shouldDeactivate && survey.IsActive)
                {
                    survey.IsActive = false;
                    anyChanges = true;
                }
            }

            if (anyChanges)
            {
                foreach (var survey in allSurveys.Where(s => !s.IsActive))
                {
                    await _surveyRepository.UpdateAsync(survey.Id, survey);
                }
            }

            // Apply filters
            var query = allSurveys.AsQueryable();

            if (request.FromDate.HasValue)
                query = query.Where(s => s.CreatedAt >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(s => s.CreatedAt <= request.ToDate.Value);

            if (request.IsActive.HasValue)
                query = query.Where(s => s.IsActive == request.IsActive.Value);

            // Correct counts
            var totalCount = query.Count();

            int totalActiveSurveys = allSurveys.Count(s => s.IsActive);

            int totalResponsesCount = allSurveys
                .SelectMany(s => s.Responses)
                .Count(r => !r.IsDeleted);

            // Pagination
            var surveys = query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // Response
            return new PagedSurveyResponseDto
            {
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalResponsesCount = totalResponsesCount,
                TotalActiveSurveys = totalActiveSurveys,

                Surveys = surveys.Select(s => new CreatorSurveyListDto
                {
                    SurveyId = s.Id,
                    Title = s.Title,
                    Description = s.Description,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt,
                    TotalResponses = s.Responses?.Count(r => !r.IsDeleted) ?? 0,
                    PublicIdentifier = s.PublicIdentifier,

                    // flag for disabling the toggle button
                    IsLocked =
                        (s.ExpireAt.HasValue && s.ExpireAt.Value < now) ||
                        (s.MaxResponses.HasValue &&
                         (s.Responses?.Count(r => !r.IsDeleted) ?? 0) >= s.MaxResponses.Value),
                    ExpireAt = s.ExpireAt,
                    MaxResponses = s.MaxResponses

                }).ToList()
            };
        }


        public async Task<SurveyAnalyticsDto> GetSurveyAnalyticsAsync(int surveyId, int userId)
        {
            try
            {
                //var survey = await _context.Surveys
                var survey = await _surveyRepository.GetQueryable()
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
            //var survey = await _context.Surveys
            var survey = await _surveyRepository.GetQueryable()
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


        //public async Task<string> ImportSurveyFromExcelAsync(
        //    ImportSurveyExcelDto dto,
        //    int creatorId)
        //{
        //    if (dto.File == null || dto.File.Length == 0)
        //        throw new BadRequestException("Excel file is required");

        //    var survey = new Survey
        //    {
        //        Title = dto.Title,
        //        Description = dto.Description,
        //        CreatedById = creatorId,
        //        IsActive = true,
        //        Questions = new List<Question>()
        //    };

        //    using var stream = new MemoryStream();
        //    await dto.File.CopyToAsync(stream);

        //    // Reset stream position
        //    stream.Position = 0;

        //    using var workbook = new XLWorkbook(stream);
        //    var worksheet = workbook.Worksheet(1);

        //    var rows = worksheet.RowsUsed().Skip(1);

        //    foreach (var row in rows)
        //    {
        //        var questionText = row.Cell(1).GetString();
        //        var questionTypeString = row.Cell(2).GetString();

        //        if (string.IsNullOrWhiteSpace(questionText))
        //            continue;

        //        // Normalize question type (handles "Multiple Choice", spaces, etc.)
        //        var normalizedType = questionTypeString
        //                                .Replace(" ", "")
        //                                .Trim();

        //        if (!Enum.TryParse<QuestionType>(
        //                normalizedType,
        //                true,
        //                out var questionType))
        //        {
        //            throw new BadRequestException(
        //                $"Invalid QuestionType: {questionTypeString}");
        //        }

        //        var question = new Question
        //        {
        //            Text = questionText,
        //            QuestionType = questionType,
        //            Options = new List<QuestionOption>()
        //        };

        //        // Handle MultipleChoice options dynamically
        //        if (questionType == QuestionType.MultipleChoice)
        //        {
        //            int lastColumn = row.LastCellUsed().Address.ColumnNumber;

        //            for (int i = 3; i <= lastColumn; i++)
        //            {
        //                var option = row.Cell(i).GetString();

        //                if (!string.IsNullOrWhiteSpace(option))
        //                {
        //                    question.Options.Add(new QuestionOption
        //                    {
        //                        OptionText = option
        //                    });
        //                }
        //            }

        //            // Ensure MultipleChoice has options
        //            if (!question.Options.Any())
        //            {
        //                throw new BadRequestException(
        //                    $"MultipleChoice question '{questionText}' must have options.");
        //            }
        //        }

        //        survey.Questions.Add(question);
        //    }

        //    //_context.Surveys.Add(survey);
        //    //await _context.SaveChangesAsync();
        //    await _surveyRepository.AddAsync(survey);

        //    return survey.PublicIdentifier;
        //}

        public async Task<string> ImportSurveyFromExcelAsync(
    ImportSurveyExcelDto dto,
    int creatorId)
        {
            if (dto.File == null || dto.File.Length == 0)
                throw new BadRequestException("Excel file is required");

            var survey = new Survey
            {
                Title = dto.Title,
                Description = dto.Description ?? string.Empty,
                CreatedById = creatorId,
                IsActive = true,
                Questions = new List<Question>(),
                ExpireAt = dto.ExpireAt,
                MaxResponses = dto.MaxResponses
            };

            using var stream = new MemoryStream();
            await dto.File.CopyToAsync(stream);
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

                var normalizedType = questionTypeString.Replace(" ", "").Trim();

                if (!Enum.TryParse<QuestionType>(normalizedType, true, out var questionType))
                {
                    throw new BadRequestException($"Invalid QuestionType: {questionTypeString}");
                }

                var question = new Question
                {
                    Text = questionText,
                    QuestionType = questionType,
                    Options = new List<QuestionOption>()
                };

                if (questionType == QuestionType.MultipleChoice)
                {
                    int lastColumn = row.LastCellUsed().Address.ColumnNumber;

                    for (int i = 3; i <= lastColumn; i++)
                    {
                        var option = row.Cell(i).GetString();

                        if (!string.IsNullOrWhiteSpace(option))
                        {
                            question.Options.Add(new QuestionOption { OptionText = option });
                        }
                    }

                    if (!question.Options.Any())
                    {
                        throw new BadRequestException(
                            $"MultipleChoice question '{questionText}' must have options.");
                    }
                }

                survey.Questions.Add(question);
            }

            await _surveyRepository.AddAsync(survey);

            return survey.PublicIdentifier;
        }





        public async Task<List<ResponseTrendDto>> GetSurveyResponseTrendAsync(int surveyId, int userId)
        {
            //var survey = await _context.Surveys
            var survey = await _surveyRepository.GetQueryable()
                .FirstOrDefaultAsync(s => s.Id == surveyId && !s.IsDeleted);

            if (survey == null)
                throw new NotFoundException("Survey not found");

            if (!IsAdmin() && survey.CreatedById != userId)
                throw new ForbiddenException("You do not own this survey");

            //var trend = await _context.Responses
            var trend = await _responsesRepository.GetQueryable()
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