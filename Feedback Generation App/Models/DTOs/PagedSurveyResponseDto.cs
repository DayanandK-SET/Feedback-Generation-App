namespace Feedback_Generation_App.Models.DTOs
{
    public class PagedSurveyResponseDto
    {
        public int TotalCount { get; set; }

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public int TotalResponsesCount { get; set; }

        public int TotalActiveSurveys { get; set; }

        public List<CreatorSurveyListDto> Surveys { get; set; } = new();
    }
}
