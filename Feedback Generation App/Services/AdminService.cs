using Feedback_Generation_App.Contexts;
using Feedback_Generation_App.Exceptions;
using Feedback_Generation_App.Interfaces;
using Feedback_Generation_App.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Feedback_Generation_App.Services
{
    public class AdminService : IAdminService
    {
        private readonly FeedbackContext _context;

        public AdminService(FeedbackContext context)
        {
            _context = context;
        }

        public async Task<List<AdminCreatorDto>> GetAllCreatorsAsync()
        {
            return await _context.Users
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
            return await _context.Surveys
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
            var survey = await _context.Surveys
                .FirstOrDefaultAsync(s => s.Id == surveyId && !s.IsDeleted);

            if (survey == null)
                throw new NotFoundException("Survey not found");

            survey.IsDeleted = true;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteCreatorAsync(int creatorId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == creatorId && u.Role == "Creator" && !u.IsDeleted);

            if (user == null)
                throw new NotFoundException("Creator not found");

            user.IsDeleted = true;

            await _context.SaveChangesAsync();
        }
    }
}