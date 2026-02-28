using Feedback_Generation_App.Contexts;
using Feedback_Generation_App.Models;
using Feedback_Generation_App.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Feedback_Generation_App.Services
{
    public class QuestionBankService
    {
        private readonly FeedbackContext _context;

        public QuestionBankService(FeedbackContext context)
        {
            _context = context;
        }

        public async Task<int> CreateQuestionAsync(CreateQuestionBankDto dto, int userId)
        {
            var question = new QuestionBank
            {
                Text = dto.Text,
                QuestionType = dto.QuestionType,
                CreatedById = userId
            };

            if (dto.Options != null && dto.Options.Any())
            {
                question.Options = dto.Options
                    .Select(o => new QuestionBankOption
                    {
                        OptionText = o
                    }).ToList();
            }

            _context.QuestionBanks.Add(question);
            await _context.SaveChangesAsync();

            return question.Id;
        }

        public async Task<QuestionBankPagedResponseDto>
            GetMyQuestionsAsync(int userId, GetQuestionBankRequestDto request)
        {
            if (request.PageNumber < 1)
                request.PageNumber = 1;

            if (request.PageSize < 1)
                request.PageSize = 10;

            var query = _context.QuestionBanks
                .Where(q => q.CreatedById == userId && !q.IsDeleted)
                .Include(q => q.Options)
                .AsQueryable();

            // Apply filtering
            if (request.QuestionType.HasValue)
            {
                query = query.Where(q =>
                    q.QuestionType == request.QuestionType.Value);
            }

            var totalCount = await query.CountAsync();

            var questions = await query
                .OrderByDescending(q => q.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(q => new QuestionBankResponseDto
                {
                    Id = q.Id,
                    Text = q.Text,
                    QuestionType = q.QuestionType,
                    Options = q.Options!
                        .Select(o => o.OptionText)
                        .ToList()
                })
                .ToListAsync();

            return new QuestionBankPagedResponseDto
            {
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                Questions = questions
            };
        }
    }
}