namespace Feedback_Generation_App.Models.DTOs
{
    public class QuestionBankResponseDto
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public QuestionType QuestionType { get; set; }
        public List<string>? Options { get; set; }
    }
}