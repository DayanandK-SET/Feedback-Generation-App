using Feedback_Generation_App.Interfaces;
using Feedback_Generation_App.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
public class SurveyController : ControllerBase
{
    private readonly ISurveyService _surveyService;

    public SurveyController(ISurveyService surveyService)
    {
        _surveyService = surveyService;
    }

    [Authorize(Roles = "Creator")]
    [HttpPost]
    public async Task<IActionResult> CreateSurvey(CreateSurveyDto dto)
    {
        int userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value
        );

        var publicId = await _surveyService.CreateSurvey(dto, userId);

        return Ok(new
        {
            Message = "Survey created successfully",
            PublicLink = $"http://localhost:5215/api/Public/{publicId}"
        });
    }
}   