namespace Feedback_Generation_App.Models.DTOs
{
    public class QuestionBankPagedResponseDto
    {
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        public List<QuestionBankResponseDto> Questions { get; set; }
            = new();
    }
}