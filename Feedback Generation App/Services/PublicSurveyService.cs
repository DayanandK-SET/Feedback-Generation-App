using Feedback_Generation_App.Contexts;
using Feedback_Generation_App.Interfaces;
using Feedback_Generation_App.Models;
using Feedback_Generation_App.Models.DTOs;
using Microsoft.EntityFrameworkCore;

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
            .FirstOrDefaultAsync(s =>
                s.PublicIdentifier == publicIdentifier &&
                s.IsActive &&
                !s.IsDeleted);

        if (survey == null)
            throw new Exception("Survey not available");

        var response = new Response
        {
            SurveyId = survey.Id,
            Answers = new List<Answer>()
        };

        foreach (var ans in dto.Answers)
        {
            response.Answers!.Add(new Answer
            {
                QuestionId = ans.QuestionId,

                // Since your model only has AnswerText,
                // we must map everything into AnswerText

                AnswerText =
                    ans.TextAnswer ??
                    ans.RatingValue?.ToString() ??
                    ans.SelectedOptionId?.ToString() ??
                    string.Empty
            });
        }

        _context.Responses.Add(response);
        await _context.SaveChangesAsync();
    }
}