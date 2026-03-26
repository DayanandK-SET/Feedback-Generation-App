//using Feedback_Generation_App.Interfaces;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//namespace Feedback_Generation_App.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    [Authorize(Roles = "Admin")]
//    public class AdminController : ControllerBase
//    {
//        private readonly IAdminService _adminService;

//        public AdminController(IAdminService adminService)
//        {
//            _adminService = adminService;
//        }

//        [HttpGet("creators")]
//        public async Task<IActionResult> GetAllCreators()
//        {
//            var creators = await _adminService.GetAllCreatorsAsync();
//            return Ok(creators);
//        }

//        [HttpGet("surveys")]
//        public async Task<IActionResult> GetAllSurveys()
//        {
//            var surveys = await _adminService.GetAllSurveysAsync();
//            return Ok(surveys);
//        }

//        [HttpDelete("survey/{id}")]
//        public async Task<IActionResult> DeleteSurvey(int id)
//        {
//            await _adminService.DeleteSurveyAsync(id);
//            return Ok(new { message = "Survey Deleted successfully" });
//        }

//        //[HttpDelete("creator/{id}")]
//        //public async Task<IActionResult> DeleteCreator(int id)
//        //{
//        //    await _adminService.DeleteCreatorAsync(id);
//        //    return Ok(new { message = "Creator Deleted successfully" });
//        //}


//        [HttpPatch("creator/{id}/toggle-status")]
//        public async Task<IActionResult> ToggleCreatorStatus(int id)
//        {
//            await _adminService.ToggleCreatorStatusAsync(id);
//            return Ok(new { Message = "Creator status updated successfully" });
//        }


//        [HttpGet("audit-logs")]
//        public async Task<IActionResult> GetAuditLogs()
//        {
//            var logs = await _adminService.GetAuditLogsAsync();
//            return Ok(logs);
//        }
//    }
//}


using Feedback_Generation_App.Interfaces;
using Feedback_Generation_App.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Feedback_Generation_App.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        // ── Existing GET endpoints — NOT touched ──────────────────

        [HttpGet("creators")]
        public async Task<IActionResult> GetAllCreators()
        {
            var creators = await _adminService.GetAllCreatorsAsync();
            return Ok(creators);
        }

        [HttpGet("surveys")]
        public async Task<IActionResult> GetAllSurveys()
        {
            var surveys = await _adminService.GetAllSurveysAsync();
            return Ok(surveys);
        }

        [HttpDelete("survey/{id}")]
        public async Task<IActionResult> DeleteSurvey(int id)
        {
            await _adminService.DeleteSurveyAsync(id);
            return Ok(new { message = "Survey Deleted successfully" });
        }

        [HttpPatch("creator/{id}/toggle-status")]
        public async Task<IActionResult> ToggleCreatorStatus(int id)
        {
            await _adminService.ToggleCreatorStatusAsync(id);
            return Ok(new { Message = "Creator status updated successfully" });
        }

        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs()
        {
            var logs = await _adminService.GetAuditLogsAsync();
            return Ok(logs);
        }

        // ── NEW: paged + filtered endpoints ──────────────────────

        // POST /api/Admin/creators/search
        [HttpPost("creators/search")]
        public async Task<IActionResult> SearchCreators(
            [FromBody] GetAdminCreatorsRequestDto request)
        {
            var result = await _adminService.SearchCreatorsAsync(request);
            return Ok(result);
        }

        // POST /api/Admin/surveys/search
        [HttpPost("surveys/search")]
        public async Task<IActionResult> SearchSurveys(
            [FromBody] GetAdminSurveysRequestDto request)
        {
            var result = await _adminService.SearchSurveysAsync(request);
            return Ok(result);
        }
    }
}
