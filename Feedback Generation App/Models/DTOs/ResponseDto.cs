namespace Feedback_Generation_App.Models.DTOs
{
    public class ResponseDto
    {
        public int ResponseId { get; set; }
        public DateTime SubmittedAt { get; set; }
        public List<AnswerDto>? Answers { get; set; }
    }
}
