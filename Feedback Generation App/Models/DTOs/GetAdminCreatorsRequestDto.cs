namespace Feedback_Generation_App.Models.DTOs
{
    public class GetAdminCreatorsRequestDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 8;
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
    }
}
