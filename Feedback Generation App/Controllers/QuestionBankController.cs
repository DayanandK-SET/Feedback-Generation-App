using Feedback_Generation_App.Models;
using Feedback_Generation_App.Models.DTOs;
using Feedback_Generation_App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Feedback_Generation_App.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Creator")]
    public class QuestionBankController : ControllerBase
    {
        private readonly QuestionBankService _service;

        public QuestionBankController(QuestionBankService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> CreateQuestion(CreateQuestionBankDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized("Invalid token");

            var userId = int.Parse(userIdClaim.Value);

            var questionId = await _service.CreateQuestionAsync(dto, userId);

            return Ok(new { QuestionId = questionId });
        }

        [HttpGet("my-questions")]
        public async Task<IActionResult> GetMyQuestions(
            [FromQuery] GetQuestionBankRequestDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized("Invalid token");

            var userId = int.Parse(userIdClaim.Value);

            var result = await _service.GetMyQuestionsAsync(userId, request);

            return Ok(result);
        }
    }
}