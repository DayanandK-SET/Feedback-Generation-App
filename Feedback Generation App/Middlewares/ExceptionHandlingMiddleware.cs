using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Feedback_Generation_App.Contexts;
using Feedback_Generation_App.Exceptions;
using Feedback_Generation_App.Models;

namespace Feedback_Generation_App.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }


        public async Task InvokeAsync(HttpContext context, FeedbackContext dbContext)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, dbContext);
            }
        }

        private static async Task HandleExceptionAsync(
            HttpContext context,
            Exception exception,
            FeedbackContext dbContext)
        {
            HttpStatusCode statusCode;
            string message = exception.Message;

            switch (exception)
            {
                case NotFoundException:
                    statusCode = HttpStatusCode.NotFound;
                    break;

                case BadRequestException:
                    statusCode = HttpStatusCode.BadRequest;
                    break;

                case ForbiddenException:
                    statusCode = HttpStatusCode.Forbidden;
                    break;

                case UnAuthorizedException:
                    statusCode = HttpStatusCode.Unauthorized;
                    break;

                default:
                    statusCode = HttpStatusCode.InternalServerError;
                    message = "An unexpected error occurred.";
                    break;
            }

            // Read user details from HttpContext.User claims.
            // JWT middleware already decoded these before we get here.
            // Will be null for anonymous requests (public survey endpoints).
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            int? loggedUserId = int.TryParse(userIdClaim, out var parsedId)
                ? parsedId
                : null;

            var loggedUsername = context.User.FindFirst(ClaimTypes.Name)?.Value;
            var loggedUserRole = context.User.FindFirst(ClaimTypes.Role)?.Value;

            
            try
            {
                var log = new Log
                {
                    StatusCode = (int)statusCode,
                    ExceptionType = exception.GetType().Name,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace,
                    Method = context.Request.Method,
                    Path = context.Request.Path,
                    QueryString = context.Request.QueryString.HasValue
                                        ? context.Request.QueryString.Value
                                        : null,
                    UserId = loggedUserId,
                    Username = loggedUsername,
                    UserRole = loggedUserRole,
                    OccurredAt = DateTime.UtcNow
                };

                dbContext.Logs.Add(log);
                await dbContext.SaveChangesAsync();
            }
            catch
            {
                // If logging fails, we still return the error response to the client.
            }

            // Response shape is unchanged from the original
            var response = new
            {
                success = false,
                message = message
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response)
            );
        }
    }
}
