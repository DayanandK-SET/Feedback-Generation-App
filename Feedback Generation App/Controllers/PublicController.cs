using Feedback_Generation_App.Interfaces;
using Feedback_Generation_App.Models.DTOs;
using Feedback_Generation_App.Services;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class PublicController : ControllerBase
{
    private readonly IPublicSurveyService _service;

    public PublicController(IPublicSurveyService service)
    {
        _service = service;
    }

    [HttpGet("{publicIdentifier}")]
    public async Task<IActionResult> GetSurvey(string publicIdentifier)
    {
        var survey = await _service.GetSurvey(publicIdentifier);

        if (survey == null)
            return NotFound("Survey not found or inactive");

        return Ok(survey);
    }

    [HttpPost("{publicIdentifier}/submit")]
    public async Task<IActionResult> SubmitSurvey(string publicIdentifier, SubmitSurveyDto dto)
    {
        try
        {
            await _service.SubmitSurvey(publicIdentifier, dto);
            return Ok(new { message = "Survey submitted successfully" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}