using Feedback_Generation_App.Models;

namespace Feedback_Generation_App.Models.DTOs
{
    public class GetQuestionBankRequestDto
    {
        public QuestionType? QuestionType { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}