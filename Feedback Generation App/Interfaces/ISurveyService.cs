using Feedback_Generation_App.Models.DTOs;

namespace Feedback_Generation_App.Interfaces
{
    public interface ISurveyService
    {
        Task<string> CreateSurvey(CreateSurveyDto dto, int creatorId);
    }
}
