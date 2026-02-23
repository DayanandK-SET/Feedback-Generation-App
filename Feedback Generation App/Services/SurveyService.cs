using Feedback_Generation_App.Contexts;
using Feedback_Generation_App.Interfaces;
using Feedback_Generation_App.Models;
using Feedback_Generation_App.Models.DTOs;

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

            foreach (var questionDto in dto.Questions)
            {
                var question = new Question
                {
                    Text = questionDto.Text,
                    QuestionType = questionDto.QuestionType
                };

                if (questionDto.Options != null && questionDto.Options.Any())
                {
                    question.Options = questionDto.Options
                        .Select(o => new QuestionOption
                        {
                            OptionText = o
                        }).ToList();
                }

                survey.Questions ??= new List<Question>();
                survey.Questions.Add(question);
            }

            _context.Surveys.Add(survey);
            await _context.SaveChangesAsync();

            return survey.PublicIdentifier;
        }
    }
}
