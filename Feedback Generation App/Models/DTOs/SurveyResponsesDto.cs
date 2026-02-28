namespace Feedback_Generation_App.Models.DTOs
{
    public class SurveyResponsesDto
    {
        public int SurveyId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int TotalResponses { get; set; }
        public List<ResponseDto>? Responses { get; set; }
    }
}
