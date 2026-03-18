namespace Feedback_Generation_App.Models
{
    public class Survey : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string PublicIdentifier { get; set; } = Guid.NewGuid().ToString();

        public bool IsActive { get; set; } = false;

        public int CreatedById { get; set; }
        public User? CreatedBy { get; set; }


        public DateTime? ExpireAt { get; set; }

        public int? MaxResponses { get; set; }

        public ICollection<Question>? Questions { get; set; }
        public ICollection<Response>? Responses { get; set; }
    }
}