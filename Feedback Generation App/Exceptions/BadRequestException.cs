using Microsoft.AspNetCore.Mvc;

namespace Feedback_Generation_App.Exceptions
{
    public class BadRequestException : Exception

    {
        public BadRequestException(string message) : base(message) { }
    }
}
