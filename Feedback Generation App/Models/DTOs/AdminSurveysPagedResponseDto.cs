namespace Feedback_Generation_App.Models.DTOs
{
    public class AdminSurveysPagedResponseDto
    {
        public int TotalCount { get; set; }        // filtered count
        public int TotalAllSurveys { get; set; }   // overall count (for stats strip)
        public int TotalActiveSurveys { get; set; } // overall active count (for stats strip)
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public List<AdminSurveyDto> Surveys { get; set; } = new();
    }
}