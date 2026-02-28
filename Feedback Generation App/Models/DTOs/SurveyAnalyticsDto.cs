namespace Feedback_Generation_App.Models.DTOs
{
    public class SurveyAnalyticsDto
    {
        public int SurveyId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int TotalResponses { get; set; }
        public List<QuestionAnalyticsDto> Questions { get; set; } = new();
    }

    public class QuestionAnalyticsDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public QuestionType QuestionType { get; set; }

        public List<OptionAnalyticsDto>? Options { get; set; }

        public double? AverageRating { get; set; }
        public int? MinRating { get; set; }
        public int? MaxRating { get; set; }

        public List<string>? TextAnswers { get; set; }
    }

    public class OptionAnalyticsDto
    {
        public string OptionText { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
