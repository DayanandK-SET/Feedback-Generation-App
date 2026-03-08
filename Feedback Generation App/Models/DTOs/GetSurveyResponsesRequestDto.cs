namespace Feedback_Generation_App.Models.DTOs
{
    public class GetSurveyResponsesRequestDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public QuestionType? QuestionType { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
