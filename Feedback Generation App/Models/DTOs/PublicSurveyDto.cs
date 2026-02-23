namespace Feedback_Generation_App.Models.DTOs
{
    public class PublicSurveyDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public List<PublicQuestionDto> Questions { get; set; } = new();
    }
}
