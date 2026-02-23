namespace Feedback_Generation_App.Models.DTOs
{
    public class TokenPayloadDto
    {
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        public int UserId { get; set; }
    }
}
