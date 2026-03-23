using Feedback_Generation_App.Models.DTOs;

namespace Feedback_Generation_App.Interfaces
{
    public interface ISurveyService
    {
        Task<string> CreateSurvey(CreateSurveyDto dto, int creatorId);

        Task<SurveyResponsesDto> GetSurveyResponsesAsync(int surveyId, int userId, GetSurveyResponsesRequestDto request);

        Task DeleteSurveyAsync(int surveyId, int userId);

        Task ToggleSurveyStatusAsync(int surveyId, int userId);

        Task UpdateSurveyAsync(int surveyId, int userId, UpdateSurveyDto dto);

        Task<PagedSurveyResponseDto> GetCreatorSurveysAsync(int userId, GetMySurveysRequestDto request);

        Task<SurveyAnalyticsDto> GetSurveyAnalyticsAsync(int surveyId, int userId);

        Task<byte[]> ExportResponsesToExcelAsync(int surveyId, int userId);


        Task<string> ImportSurveyFromExcelAsync(ImportSurveyExcelDto dto, int creatorId);

        Task<List<ResponseTrendDto>> GetSurveyResponseTrendAsync(int surveyId, int userId);

    }
}
