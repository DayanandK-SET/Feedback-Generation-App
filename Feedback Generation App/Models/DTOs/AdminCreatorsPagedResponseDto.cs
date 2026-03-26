namespace Feedback_Generation_App.Models.DTOs
{
    public class AdminCreatorsPagedResponseDto
    {
        public int TotalCount { get; set; }        // filtered count
        public int TotalAllCreators { get; set; }  // overall count (for stats strip)
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public List<AdminCreatorDto> Creators { get; set; } = new();
    }
}