namespace Feedback_Generation_App.Models
{
    public class QuestionBank : BaseEntity
    {
        public string Text { get; set; } = string.Empty;

        public QuestionType QuestionType { get; set; }

        public int CreatedById { get; set; }
        public User? CreatedBy { get; set; }

        public ICollection<QuestionBankOption>? Options { get; set; }
    }
}