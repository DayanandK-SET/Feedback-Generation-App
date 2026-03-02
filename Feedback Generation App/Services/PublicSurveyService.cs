using Feedback_Generation_App.Contexts;
using Feedback_Generation_App.Exceptions;
using Feedback_Generation_App.Interfaces;
using Feedback_Generation_App.Models;
using Feedback_Generation_App.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Feedback_Generation_App.Services
{
    public class PublicSurveyService : IPublicSurveyService
    {
        private readonly FeedbackContext _context;

        public PublicSurveyService(FeedbackContext context)
        {
            _context = context;
        }

        public async Task<PublicSurveyDto?> GetSurvey(string publicIdentifier)
        {
            var survey = await _context.Surveys
                .Include(s => s.Questions!)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(s =>
                    s.PublicIdentifier == publicIdentifier &&
                    s.IsActive &&
                    !s.IsDeleted);

            if (survey == null)
                return null;

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
            var survey = await _context.Surveys
                .Include(s => s.Questions)
                .FirstOrDefaultAsync(s =>
                    s.PublicIdentifier == publicIdentifier &&
                    s.IsActive &&
                    !s.IsDeleted);

            if (survey == null)
                throw new BadRequestException("Survey not available");

            if (string.IsNullOrWhiteSpace(dto.ResponseToken))
                throw new ArgumentException("Response token is required.");

            // 🔐 Duplicate Protection
            var existingResponse = await _context.Responses
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

                var answer = new Answer
                {
                    QuestionId = ans.QuestionId
                };

                if (!string.IsNullOrEmpty(ans.TextAnswer))
                {
                    answer.AnswerText = ans.TextAnswer;
                }
                else if (ans.RatingValue.HasValue)
                {
                    answer.AnswerText = ans.RatingValue.Value.ToString();
                }
                else if (ans.SelectedOptionId.HasValue)
                {
                    answer.SelectedOptionId = ans.SelectedOptionId.Value;
                }

                response.Answers!.Add(answer);
            }

            _context.Responses.Add(response);
            await _context.SaveChangesAsync();
        }
    }
}