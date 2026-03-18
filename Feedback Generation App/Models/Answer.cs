namespace Feedback_Generation_App.Models
{
    public class Answer : BaseEntity
    {
        public int QuestionId { get; set; }
        public Question? Question { get; set; }

        public int ResponseId { get; set; }
        public Response? Response { get; set; }

        public string? AnswerText { get; set; }

        public int? SelectedOptionId { get; set; }
        public QuestionOption? SelectedOption { get; set; }
    }
}