using Feedback_Generation_App.Exceptions;
using Feedback_Generation_App.Interfaces;
using Feedback_Generation_App.Models;
using Feedback_Generation_App.Models.DTOs;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Feedback_Generation_App.Services
{
    public class AdminService : IAdminService
    {
        private readonly IRepository<int, User> _userRepository;
        private readonly IRepository<int, Survey> _surveyRepository;
        private readonly IRepository<int, AuditLog> _auditLogRepository;

        public AdminService(
            IRepository<int, User> userRepository,
            IRepository<int, Survey> surveyRepository,
            IRepository<int, AuditLog> auditLogRepository)
        {
            _userRepository = userRepository;
            _surveyRepository = surveyRepository;
            _auditLogRepository = auditLogRepository;
        }

        public async Task<List<AdminCreatorDto>> GetAllCreatorsAsync()
        {
            return await _userRepository.GetQueryable()
                .Where(u => u.Role == "Creator" && !u.IsDeleted)
                .Select(u => new AdminCreatorDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email
                })
                .ToListAsync();
        }

        public async Task<List<AdminSurveyDto>> GetAllSurveysAsync()
        {
            return await _surveyRepository.GetQueryable()
                .Where(s => !s.IsDeleted)
                .Include(s => s.CreatedBy)
                .Select(s => new AdminSurveyDto
                {
                    Id = s.Id,
                    Title = s.Title,
                    IsActive = s.IsActive,
                    Creator = s.CreatedBy != null ? s.CreatedBy.Username : "Unknown"
                })
                .ToListAsync();
        }

        public async Task DeleteSurveyAsync(int surveyId)
        {
            var survey = await _surveyRepository.GetQueryable()
                .FirstOrDefaultAsync(s => s.Id == surveyId && !s.IsDeleted);

            if (survey == null)
                throw new NotFoundException("Survey not found");

            survey.IsDeleted = true;
            await _surveyRepository.UpdateAsync(surveyId, survey);

            
        }

        public async Task DeleteCreatorAsync(int creatorId)
        {
            var user = await _userRepository.GetQueryable()
                .FirstOrDefaultAsync(u =>
                    u.Id == creatorId &&
                    u.Role == "Creator" &&
                    !u.IsDeleted);

            if (user == null)
                throw new NotFoundException("Creator not found");

            user.IsDeleted = true;
            await _userRepository.UpdateAsync(creatorId, user);
        }

        // Returns all audit logs, most recent first
        public async Task<List<AuditLogDto>> GetAuditLogsAsync()
        {
            return await _auditLogRepository.GetQueryable()
                .OrderByDescending(a => a.PerformedAt)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    Action = a.Action,
                    SurveyId = a.SurveyId,
                    SurveyTitle = a.SurveyTitle,
                    PerformedBy = a.PerformedBy,
                    PerformedAt = a.PerformedAt
                })
                .ToListAsync();
        }
    }
}
