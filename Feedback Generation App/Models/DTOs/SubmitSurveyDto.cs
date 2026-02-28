namespace Feedback_Generation_App.Models.DTOs
{
    public class SubmitSurveyDto
    {
        public List<SubmitAnswerDto> Answers { get; set; } = new();

        public string ResponseToken { get; set; } = string.Empty;
    }
}
