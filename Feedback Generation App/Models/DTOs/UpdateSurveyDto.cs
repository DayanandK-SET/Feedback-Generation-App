namespace Feedback_Generation_App.Models.DTOs
{
    public class UpdateSurveyDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public DateTime? ExpireAt { get; set; }
        public int? MaxResponses { get; set; }
    }
}
