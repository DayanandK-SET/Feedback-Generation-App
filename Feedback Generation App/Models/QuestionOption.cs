namespace Feedback_Generation_App.Models
{
    public class QuestionOption : BaseEntity
    {
        public string OptionText { get; set; } = string.Empty;

        public int QuestionId { get; set; }
        public Question? Question { get; set; }
    }
}