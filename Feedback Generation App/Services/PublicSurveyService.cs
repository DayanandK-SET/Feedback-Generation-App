using Feedback_Generation_App.Exceptions;
using Feedback_Generation_App.Interfaces;
using Feedback_Generation_App.Models;
using Feedback_Generation_App.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Feedback_Generation_App.Services
{
    public class PublicSurveyService : IPublicSurveyService
    {
        private readonly IRepository<int, Survey> _surveyRepository;
        private readonly IRepository<int, Response> _responseRepository;

        public PublicSurveyService(
            IRepository<int, Survey> surveyRepository,
            IRepository<int, Response> responseRepository)
        {
            _surveyRepository = surveyRepository;
            _responseRepository = responseRepository;
        }

        public async Task<PublicSurveyDto?> GetSurvey(string publicIdentifier)
        {
            var survey = await _surveyRepository.GetQueryable()
                .Include(s => s.Questions!)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(s =>
                    s.PublicIdentifier == publicIdentifier &&
                    s.IsActive &&
                    !s.IsDeleted);

            if (survey == null)
                return null;

            if (survey.ExpireAt.HasValue && survey.ExpireAt.Value < DateTime.UtcNow)
                throw new BadRequestException("This survey has expired");

            return new PublicSurveyDto
            {
                Title = survey.Title,
                Description = survey.Description,
                Questions = survey.Questions!.Select(q => new PublicQuestionDto
                {
                    QuestionId = q.Id,
                    Text = q.Text,
                    QuestionType = q.QuestionType,
                    Options = q.Options?.Select(o => new PublicOptionDto
                    {
                        OptionId = o.Id,
                        OptionText = o.OptionText
                    }).ToList()
                }).ToList()
            };
        }

        public async Task SubmitSurvey(string publicIdentifier, SubmitSurveyDto dto)
        {
            var survey = await _surveyRepository.GetQueryable()
                .Include(s => s.Questions)
                .FirstOrDefaultAsync(s =>
                    s.PublicIdentifier == publicIdentifier &&
                    s.IsActive &&
                    !s.IsDeleted);

            if (survey == null)
                throw new BadRequestException("Survey not available");

            // Response limit check
            if (survey.MaxResponses.HasValue)
            {
                var responseCount = await _responseRepository.GetQueryable()
                    .CountAsync(r => r.SurveyId == survey.Id && !r.IsDeleted);

                if (responseCount >= survey.MaxResponses.Value)
                    throw new BadRequestException("Survey response limit reached");
            }

            // Expiry check
            if (survey.ExpireAt.HasValue && survey.ExpireAt.Value < DateTime.UtcNow)
                throw new BadRequestException("This survey has expired");

            if (string.IsNullOrWhiteSpace(dto.ResponseToken))
                throw new ArgumentException("Response token is required.");

            // Duplicate protection
            var existingResponse = await _responseRepository.GetQueryable()
                .FirstOrDefaultAsync(r =>
                    r.SurveyId == survey.Id &&
                    r.ResponseToken == dto.ResponseToken &&
                    !r.IsDeleted);

            if (existingResponse != null)
                throw new ArgumentException("You have already submitted this survey.");

            var response = new Response
            {
                SurveyId = survey.Id,
                ResponseToken = dto.ResponseToken,
                Answers = new List<Answer>()
            };

            foreach (var ans in dto.Answers)
            {
                var question = survey.Questions!
                    .FirstOrDefault(q => q.Id == ans.QuestionId);

                if (question == null)
                    throw new ArgumentException($"Invalid question id: {ans.QuestionId}");

                var answer = new Answer { QuestionId = ans.QuestionId };

                if (!string.IsNullOrEmpty(ans.TextAnswer))
                    answer.AnswerText = ans.TextAnswer;
                else if (ans.RatingValue.HasValue)
                    answer.AnswerText = ans.RatingValue.Value.ToString();
                else if (ans.SelectedOptionId.HasValue)
                    answer.SelectedOptionId = ans.SelectedOptionId.Value;

                response.Answers!.Add(answer);
            }

            await _responseRepository.AddAsync(response);
        }
    }
}