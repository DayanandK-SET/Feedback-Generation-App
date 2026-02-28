using System.Net;
using System.Text.Json;
using Feedback_Generation_App.Exceptions;

namespace Feedback_Generation_App.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(
            HttpContext context,
            Exception exception)
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

            var response = new
            {
                success = false,
                message = message
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            return context.Response.WriteAsync(
                JsonSerializer.Serialize(response)
            );
        }
    }
}