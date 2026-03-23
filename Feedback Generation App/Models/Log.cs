namespace Feedback_Generation_App.Models
{
    public class Log
    {
        public int Id { get; set; }

        public int StatusCode { get; set; }

        public string ExceptionType { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string? StackTrace { get; set; }

        public string Method { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;

        public string? QueryString { get; set; }

        // Filled from HttpContext.User claims (null for anonymous requests)
        public int? UserId { get; set; }

        public string? Username { get; set; }

        public string? UserRole { get; set; }

        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}
