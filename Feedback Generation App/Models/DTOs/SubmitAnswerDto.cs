namespace Feedback_Generation_App.Models.DTOs
{
    public class SubmitAnswerDto
    {
        public int QuestionId { get; set; }

        public string? TextAnswer { get; set; }

        public int? SelectedOptionId { get; set; }

        public int? RatingValue { get; set; }
    }
}
