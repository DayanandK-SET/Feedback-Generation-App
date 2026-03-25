namespace Feedback_Generation_App.Models.DTOs
{
    public class CreatorSurveyListDto
    {
        public int SurveyId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalResponses { get; set; }

        public bool IsLocked { get; set; }
        public string PublicIdentifier { get; set; } = string.Empty;

        public DateTime? ExpireAt { get; set; }
        public int? MaxResponses { get; set; }
    }
}
