using Feedback_Generation_App.Exceptions;
using Feedback_Generation_App.Interfaces;
using Feedback_Generation_App.Models;
using Feedback_Generation_App.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Feedback_Generation_App.Services
{
    public class QuestionBankService
    {
        private readonly IRepository<int, QuestionBank> _questionBankRepository;

        public QuestionBankService(IRepository<int, QuestionBank> questionBankRepository)
        {
            _questionBankRepository = questionBankRepository;
        }

        public async Task<List<int>> CreateQuestionsAsync(
            List<CreateQuestionBankDto> dtos,
            int userId)
        {
            if (dtos == null || !dtos.Any())
                throw new BadRequestException("At least one question is required.");

            var createdIds = new List<int>();

            foreach (var dto in dtos)
            {
                if (string.IsNullOrWhiteSpace(dto.Text))
                    throw new BadRequestException("Question text is required.");

                var question = new QuestionBank
                {
                    Text = dto.Text,
                    QuestionType = dto.QuestionType,
                    CreatedById = userId,
                    CreatedAt = DateTime.UtcNow
                };

                if (dto.Options != null && dto.Options.Any())
                {
                    question.Options = dto.Options
                        .Select(o => new QuestionBankOption
                        {
                            OptionText = o
                        }).ToList();
                }

                // AddAsync saves each question individually
                await _questionBankRepository.AddAsync(question);
                createdIds.Add(question.Id);
            }

            return createdIds;
        }

        public async Task<QuestionBankPagedResponseDto> GetMyQuestionsAsync(
            int userId, bool isAdmin, GetQuestionBankRequestDto request)
        {
            if (request.PageNumber < 1) request.PageNumber = 1;
            if (request.PageSize < 1) request.PageSize = 10;

            var query = _questionBankRepository.GetQueryable()
                .Where(q => !q.IsDeleted)
                .Include(q => q.Options)
                .AsQueryable();

            if (!isAdmin)
                query = query.Where(q => q.CreatedById == userId);

            if (request.QuestionType.HasValue)
                query = query.Where(q => q.QuestionType == request.QuestionType.Value);

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