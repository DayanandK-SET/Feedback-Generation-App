using Microsoft.AspNetCore.Http;

namespace Feedback_Generation_App.Models.DTOs
{
    public class ImportSurveyExcelDto
    {
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public IFormFile File { get; set; } = null!;


        public DateTime? ExpireAt { get; set; }

        public int? MaxResponses { get; set; }
    }
}