namespace Feedback_Generation_App.Models.DTOs
{
    public class AdminCreatorDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // ✅ true = active (IsDeleted=false), false = inactive (IsDeleted=true)
        public bool IsActive { get; set; }
    }
}