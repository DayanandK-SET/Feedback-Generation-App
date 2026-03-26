namespace Feedback_Generation_App.Models.DTOs
{
    public class GetAdminSurveysRequestDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 8;
        public string? Search { get; set; }    // filter by title
        public string? Creator { get; set; }   // filter by creator username
        public bool? IsActive { get; set; }    // filter by status
    }
}