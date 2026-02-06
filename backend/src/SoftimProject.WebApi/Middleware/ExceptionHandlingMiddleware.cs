using System.Text.Json;
using FluentValidation;
using SoftimProject.Application.Common;

namespace SoftimProject.WebApi.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            ValidationException validationEx => (StatusCodes.Status400BadRequest, new ErrorResponse(
                "Validation Failed",
                validationEx.Errors.Select(e => e.ErrorMessage).ToArray())),

            NotFoundException notFoundEx => (StatusCodes.Status404NotFound, new ErrorResponse(notFoundEx.Message)),

            UnauthorizedAccessException unauthorizedEx => (StatusCodes.Status403Forbidden, new ErrorResponse(unauthorizedEx.Message)),

            ForbiddenAccessException => (StatusCodes.Status403Forbidden, new ErrorResponse("You do not have permission to perform this action.")),

            _ => (StatusCodes.Status500InternalServerError, new ErrorResponse("An unexpected error occurred."))
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception");
        }
        else
        {
            logger.LogWarning(exception, "Handled exception: {Message}", exception.Message);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }
}

public record ErrorResponse(string Message, string[]? Errors = null);
