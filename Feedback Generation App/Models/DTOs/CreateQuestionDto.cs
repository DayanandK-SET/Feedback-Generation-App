namespace Feedback_Generation_App.Models.DTOs
{
    public class CreateQuestionDto
    {
        public string? Text { get; set; } = string.Empty;

        public QuestionType? QuestionType { get; set; }

        public List<string>? Options { get; set; }

        public int? QuestionBankId { get; set; }
    }
}
