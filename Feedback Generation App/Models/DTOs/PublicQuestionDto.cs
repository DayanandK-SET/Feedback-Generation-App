namespace Feedback_Generation_App.Models.DTOs
{
    public class PublicQuestionDto
    {
        public int QuestionId { get; set; }

        public string Text { get; set; } = string.Empty;

        public QuestionType QuestionType { get; set; }

        public List<PublicOptionDto>? Options { get; set; }
    }
}
