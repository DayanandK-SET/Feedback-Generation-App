namespace Feedback_Generation_App.Models
{
    public class Survey : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Public link identifier
        public string PublicIdentifier { get; set; } = Guid.NewGuid().ToString();

        public bool IsActive { get; set; } = false;

        // Creator
        public int CreatedById { get; set; }
        public User? CreatedBy { get; set; }

        // Navigation
        public ICollection<Question>? Questions { get; set; }
        public ICollection<Response>? Responses { get; set; }
    }
}