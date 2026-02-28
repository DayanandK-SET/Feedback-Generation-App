namespace Feedback_Generation_App.Models
{
    public class Response : BaseEntity
    {
        public int SurveyId { get; set; }
        public Survey? Survey { get; set; }

        public ICollection<Answer>? Answers { get; set; }

        public string? ResponseToken { get; set; }
    }
}