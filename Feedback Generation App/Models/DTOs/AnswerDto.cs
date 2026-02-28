namespace Feedback_Generation_App.Models.DTOs
{
    public class AnswerDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }
}
