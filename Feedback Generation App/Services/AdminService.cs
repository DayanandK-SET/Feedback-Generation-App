//using Feedback_Generation_App.Exceptions;
//using Feedback_Generation_App.Interfaces;
//using Feedback_Generation_App.Models;
//using Feedback_Generation_App.Models.DTOs;
//using Microsoft.AspNetCore.Http.HttpResults;
//using Microsoft.EntityFrameworkCore;

//namespace Feedback_Generation_App.Services
//{
//    public class AdminService : IAdminService
//    {
//        private readonly IRepository<int, User> _userRepository;
//        private readonly IRepository<int, Survey> _surveyRepository;
//        private readonly IRepository<int, AuditLog> _auditLogRepository;

//        public AdminService(
//            IRepository<int, User> userRepository,
//            IRepository<int, Survey> surveyRepository,
//            IRepository<int, AuditLog> auditLogRepository)
//        {
//            _userRepository = userRepository;
//            _surveyRepository = surveyRepository;
//            _auditLogRepository = auditLogRepository;
//        }

//        public async Task<List<AdminCreatorDto>> GetAllCreatorsAsync()
//        {
//            return await _userRepository.GetQueryable()
//                .Where(u => u.Role == "Creator" && !u.IsDeleted)
//                .Select(u => new AdminCreatorDto
//                {
//                    Id = u.Id,
//                    Username = u.Username,
//                    Email = u.Email
//                })
//                .ToListAsync();
//        }

//        public async Task<List<AdminSurveyDto>> GetAllSurveysAsync()
//        {
//            return await _surveyRepository.GetQueryable()
//                .Where(s => !s.IsDeleted)
//                .Include(s => s.CreatedBy)
//                .Select(s => new AdminSurveyDto
//                {
//                    Id = s.Id,
//                    Title = s.Title,
//                    IsActive = s.IsActive,
//                    Creator = s.CreatedBy != null ? s.CreatedBy.Username : "Unknown"
//                })
//                .ToListAsync();
//        }

//        public async Task DeleteSurveyAsync(int surveyId)
//        {
//            var survey = await _surveyRepository.GetQueryable()
//                .FirstOrDefaultAsync(s => s.Id == surveyId && !s.IsDeleted);

//            if (survey == null)
//                throw new NotFoundException("Survey not found");

//            survey.IsDeleted = true;
//            await _surveyRepository.UpdateAsync(surveyId, survey);


//            await _auditLogRepository.AddAsync(new AuditLog
//            {
//                Action = "Admin Deleted Survey",
//                SurveyId = survey.Id,
//                SurveyTitle = survey.Title,
//                PerformedBy = "Admin", 
//                PerformedAt = DateTime.UtcNow
//            });

//        }

//        public async Task DeleteCreatorAsync(int creatorId)
//        {
//            var user = await _userRepository.GetQueryable()
//                .FirstOrDefaultAsync(u =>
//                    u.Id == creatorId &&
//                    u.Role == "Creator" &&
//                    !u.IsDeleted);

//            if (user == null)
//                throw new NotFoundException("Creator not found");

//            user.IsDeleted = true;
//            await _userRepository.UpdateAsync(creatorId, user);

//            await _auditLogRepository.AddAsync(new AuditLog
//            {
//                Action = "Admin Deleted Creator",
//                SurveyId = 0, 
//                SurveyTitle = user.Username, 
//                PerformedBy = "Admin",
//                PerformedAt = DateTime.UtcNow
//            });


//        }

//        // Returns all audit logs, most recent first
//        public async Task<List<AuditLogDto>> GetAuditLogsAsync()
//        {
//            return await _auditLogRepository.GetQueryable()
//                .OrderByDescending(a => a.PerformedAt)
//                .Select(a => new AuditLogDto
//                {
//                    Id = a.Id,
//                    Action = a.Action,
//                    SurveyId = a.SurveyId,
//                    SurveyTitle = a.SurveyTitle,
//                    PerformedBy = a.PerformedBy,
//                    PerformedAt = a.PerformedAt
//                })
//                .ToListAsync();
//        }
//    }
//}


//using Feedback_Generation_App.Exceptions;
//using Feedback_Generation_App.Interfaces;
//using Feedback_Generation_App.Models;
//using Feedback_Generation_App.Models.DTOs;
//using Microsoft.EntityFrameworkCore;

//namespace Feedback_Generation_App.Services
//{
//    public class AdminService : IAdminService
//    {
//        private readonly IRepository<int, User> _userRepository;
//        private readonly IRepository<int, Survey> _surveyRepository;
//        private readonly IRepository<int, AuditLog> _auditLogRepository;

//        public AdminService(
//            IRepository<int, User> userRepository,
//            IRepository<int, Survey> surveyRepository,
//            IRepository<int, AuditLog> auditLogRepository)
//        {
//            _userRepository = userRepository;
//            _surveyRepository = surveyRepository;
//            _auditLogRepository = auditLogRepository;
//        }

//        // ✅ Now returns ALL creators (active + inactive) so admin can toggle them
//        // IsActive = !IsDeleted — we reuse the existing column, no migration needed
//        public async Task<List<AdminCreatorDto>> GetAllCreatorsAsync()
//        {
//            return await _userRepository.GetQueryable()
//                .Where(u => u.Role == "Creator")
//                .Select(u => new AdminCreatorDto
//                {
//                    Id = u.Id,
//                    Username = u.Username,
//                    Email = u.Email,
//                    IsActive = !u.IsDeleted   // ✅ active means not deleted
//                })
//                .ToListAsync();
//        }

//        public async Task<List<AdminSurveyDto>> GetAllSurveysAsync()
//        {
//            return await _surveyRepository.GetQueryable()
//                .Where(s => !s.IsDeleted)
//                .Include(s => s.CreatedBy)
//                .Select(s => new AdminSurveyDto
//                {
//                    Id = s.Id,
//                    Title = s.Title,
//                    IsActive = s.IsActive,
//                    Creator = s.CreatedBy != null ? s.CreatedBy.Username : "Unknown"
//                })
//                .ToListAsync();
//        }

//        public async Task DeleteSurveyAsync(int surveyId)
//        {
//            var survey = await _surveyRepository.GetQueryable()
//                .FirstOrDefaultAsync(s => s.Id == surveyId && !s.IsDeleted);

//            if (survey == null)
//                throw new NotFoundException("Survey not found");

//            survey.IsDeleted = true;
//            await _surveyRepository.UpdateAsync(surveyId, survey);
//        }

//        // ✅ NEW: Toggle creator active/inactive using existing IsDeleted column
//        // IsDeleted=false → active, IsDeleted=true → inactive
//        public async Task ToggleCreatorStatusAsync(int creatorId)
//        {
//            var user = await _userRepository.GetQueryable()
//                .FirstOrDefaultAsync(u => u.Id == creatorId && u.Role == "Creator");

//            if (user == null)
//                throw new NotFoundException("Creator not found");

//            // Flip the status
//            user.IsDeleted = !user.IsDeleted;

//            await _userRepository.UpdateAsync(creatorId, user);

//            var action = user.IsDeleted ? "Creator Deactivated" : "Creator Activated";

//            await _auditLogRepository.AddAsync(new AuditLog
//            {
//                Action = action,
//                SurveyId = 0,
//                SurveyTitle = user.Username,
//                PerformedBy = "Admin",
//                PerformedAt = DateTime.UtcNow
//            });
//        }

//        public async Task<List<AuditLogDto>> GetAuditLogsAsync()
//        {
//            return await _auditLogRepository.GetQueryable()
//                .OrderByDescending(a => a.PerformedAt)
//                .Select(a => new AuditLogDto
//                {
//                    Id = a.Id,
//                    Action = a.Action,
//                    SurveyId = a.SurveyId,
//                    SurveyTitle = a.SurveyTitle,
//                    PerformedBy = a.PerformedBy,
//                    PerformedAt = a.PerformedAt
//                })
//                .ToListAsync();
//        }
//    }
//}




using Feedback_Generation_App.Exceptions;
using Feedback_Generation_App.Interfaces;
using Feedback_Generation_App.Models;
using Feedback_Generation_App.Models.DTOs;
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

        // ── Existing methods — unchanged ──────────────────────────

        public async Task<List<AdminCreatorDto>> GetAllCreatorsAsync()
        {
            return await _userRepository.GetQueryable()
                .Where(u => u.Role == "Creator")
                .Select(u => new AdminCreatorDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    IsActive = !u.IsDeleted
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

            await _auditLogRepository.AddAsync(new AuditLog
            {
                Action = "Survey Deleted",
                SurveyId = survey.Id,
                SurveyTitle = survey.Title,
                PerformedBy = "Admin", // later you can replace with logged-in user
                PerformedAt = DateTime.UtcNow
            });
        }

        public async Task ToggleCreatorStatusAsync(int creatorId)
        {
            var user = await _userRepository.GetQueryable()
                .FirstOrDefaultAsync(u => u.Id == creatorId && u.Role == "Creator");

            if (user == null)
                throw new NotFoundException("Creator not found");

            user.IsDeleted = !user.IsDeleted;
            await _userRepository.UpdateAsync(creatorId, user);

            var action = user.IsDeleted ? "Creator Deactivated" : "Creator Activated";
            await _auditLogRepository.AddAsync(new AuditLog
            {
                Action = action,
                SurveyId = 0,
                SurveyTitle = user.Username,
                PerformedBy = "Admin",
                PerformedAt = DateTime.UtcNow
            });
        }

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

        // ── NEW: Paged + filtered creators ───────────────────────

        public async Task<AdminCreatorsPagedResponseDto> SearchCreatorsAsync(
            GetAdminCreatorsRequestDto request)
        {
            if (request.PageNumber < 1) request.PageNumber = 1;
            if (request.PageSize < 1) request.PageSize = 8;

            // Overall count for the stats strip (no filters)
            var totalAllCreators = await _userRepository.GetQueryable()
                .CountAsync(u => u.Role == "Creator");

            // Base query
            var query = _userRepository.GetQueryable()
                .Where(u => u.Role == "Creator");

            // Search filter
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var s = request.Search.ToLower();
                query = query.Where(u =>
                    u.Username.ToLower().Contains(s) ||
                    u.Email.ToLower().Contains(s));
            }

            // Status filter — IsActive maps to !IsDeleted
            if (request.IsActive.HasValue)
            {
                bool isDeleted = !request.IsActive.Value;
                query = query.Where(u => u.IsDeleted == isDeleted);
            }

            var totalCount = await query.CountAsync();

            var creators = await query
                .OrderBy(u => u.Username)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(u => new AdminCreatorDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    IsActive = !u.IsDeleted
                })
                .ToListAsync();

            return new AdminCreatorsPagedResponseDto
            {
                TotalCount = totalCount,
                TotalAllCreators = totalAllCreators,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                Creators = creators
            };
        }

        // ── NEW: Paged + filtered surveys ────────────────────────

        public async Task<AdminSurveysPagedResponseDto> SearchSurveysAsync(
            GetAdminSurveysRequestDto request)
        {
            if (request.PageNumber < 1) request.PageNumber = 1;
            if (request.PageSize < 1) request.PageSize = 8;

            // Overall counts for the stats strip (no filters)
            var totalAllSurveys = await _surveyRepository.GetQueryable()
                .CountAsync(s => !s.IsDeleted);

            var totalActiveSurveys = await _surveyRepository.GetQueryable()
                .CountAsync(s => !s.IsDeleted && s.IsActive);

            // Base query
            IQueryable<Survey> query = _surveyRepository.GetQueryable()
                .Where(s => !s.IsDeleted);

            // Search by title
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var s = request.Search.Trim();
                query = query.Where(sv =>
                    EF.Functions.Like(sv.Title, $"%{s}%"));
            }

            // Filter by creator username
            if (!string.IsNullOrWhiteSpace(request.Creator))
            {
                var c = request.Creator.Trim();
                query = query.Where(sv =>
                    sv.CreatedBy != null &&
                    EF.Functions.Like(sv.CreatedBy.Username, $"%{c}%"));
            }

            // Filter by active status
            if (request.IsActive.HasValue)
                query = query.Where(sv => sv.IsActive == request.IsActive.Value);

            var totalCount = await query.CountAsync();

            var surveys = await query
                .OrderByDescending(sv => sv.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(sv => new AdminSurveyDto
                {
                    Id = sv.Id,
                    Title = sv.Title,
                    IsActive = sv.IsActive,
                    Creator = sv.CreatedBy != null ? sv.CreatedBy.Username : "Unknown"
                })
                .ToListAsync();

            return new AdminSurveysPagedResponseDto
            {
                TotalCount = totalCount,
                TotalAllSurveys = totalAllSurveys,
                TotalActiveSurveys = totalActiveSurveys,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                Surveys = surveys
            };
        }
    }
}
