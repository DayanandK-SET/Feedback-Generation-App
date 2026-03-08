using Feedback_Generation_App.Models.DTOs;
using Feedback_Generation_App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Feedback_Generation_App.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Creator,Admin")]
    public class QuestionBankController : ControllerBase
    {
        private readonly QuestionBankService _service;

        public QuestionBankController(QuestionBankService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> CreateQuestions(List<CreateQuestionBankDto> dtos)
        {
            var userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var ids = await _service.CreateQuestionsAsync(dtos, userId);

            return Ok(new
            {
                Message = "Questions created successfully",
                QuestionIds = ids
            });
        }

        [HttpGet("my-questions")]
        public async Task<IActionResult> GetMyQuestions(
            [FromQuery] GetQuestionBankRequestDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized("Invalid token");

            var userId = int.Parse(userIdClaim.Value);

            var isAdmin = User.IsInRole("Admin");

            var result = await _service.GetMyQuestionsAsync(userId, isAdmin, request);

            return Ok(result);
        }
    }
}