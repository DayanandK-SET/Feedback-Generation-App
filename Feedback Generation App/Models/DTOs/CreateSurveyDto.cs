namespace Feedback_Generation_App.Models.DTOs
{
    public class CreateSurveyDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public List<CreateQuestionDto> Questions { get; set; } = new();
    }
}
