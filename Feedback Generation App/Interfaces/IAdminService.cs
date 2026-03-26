using Feedback_Generation_App.Models.DTOs;

namespace Feedback_Generation_App.Interfaces
{
    public interface IAdminService
    {
        Task<List<AdminCreatorDto>> GetAllCreatorsAsync();
        Task<List<AdminSurveyDto>> GetAllSurveysAsync();
        Task DeleteSurveyAsync(int surveyId);
        //Task DeleteCreatorAsync(int creatorId);
        Task ToggleCreatorStatusAsync(int creatorId);
        Task<List<AuditLogDto>> GetAuditLogsAsync();


        Task<AdminCreatorsPagedResponseDto> SearchCreatorsAsync(GetAdminCreatorsRequestDto request);
        Task<AdminSurveysPagedResponseDto> SearchSurveysAsync(GetAdminSurveysRequestDto request);
    }
}