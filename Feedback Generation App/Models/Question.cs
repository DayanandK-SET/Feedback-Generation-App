namespace Feedback_Generation_App.Models
{
    public class Question : BaseEntity
    {
        public string Text { get; set; } = string.Empty;

        public QuestionType QuestionType { get; set; }

        public int SurveyId { get; set; }
        public Survey? Survey { get; set; }

        public ICollection<QuestionOption>? Options { get; set; }
        public ICollection<Answer>? Answers { get; set; }
    }
}