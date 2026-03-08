namespace Feedback_Generation_App.Models.DTOs
{
    public class AdminSurveyDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string Creator { get; set; } = string.Empty;
    }
}