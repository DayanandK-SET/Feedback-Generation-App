namespace Feedback_Generation_App.Models.DTOs
{
    public class GetMySurveysRequestDto
    {
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public bool? IsActive { get; set; }
    }
}
