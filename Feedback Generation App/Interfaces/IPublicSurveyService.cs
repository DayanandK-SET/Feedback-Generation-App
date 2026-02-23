using Feedback_Generation_App.Models.DTOs;

namespace Feedback_Generation_App.Interfaces
{
    public interface IPublicSurveyService
    {
        Task<PublicSurveyDto?> GetSurvey(string publicIdentifier);

        Task SubmitSurvey(string publicIdentifier, SubmitSurveyDto dto);
    }
}
