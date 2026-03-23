namespace Feedback_Generation_App.Models.DTOs
{
    public class AuditLogDto
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public int SurveyId { get; set; }
        public string SurveyTitle { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public DateTime PerformedAt { get; set; }
    }
}
