using Feedback_Generation_App.Models;

namespace Feedback_Generation_App.Models
{
    public class User : BaseEntity
    {
        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
            
        public byte[] Password { get; set; } = Array.Empty<byte>();
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

        public string Role { get; set; } = string.Empty;

        public ICollection<Survey>? Surveys { get; set; }
    }
}