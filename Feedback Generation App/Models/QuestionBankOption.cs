namespace Feedback_Generation_App.Models
{
    public class QuestionBankOption : BaseEntity
    {
        public string OptionText { get; set; } = string.Empty;

        public int QuestionBankId { get; set; }
        public QuestionBank? QuestionBank { get; set; }
    }
}