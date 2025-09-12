using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace PopCultureMashup.Api.Middleware;

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
            logger.LogError(ex, "Unhandled exception. TraceId={TraceId}", context.TraceIdentifier);

            if (context.Response.HasStarted)
            {
                logger.LogWarning(
                    "The response has already started, the error handling middleware will not be executed.");
                throw;
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            Timestamp = DateTime.UtcNow,
            TraceId = context.TraceIdentifier
        };

        switch (exception)
        {
            case ArgumentException argEx:
                response.Message = argEx.Message;
                context.Response.StatusCode = response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case InvalidOperationException invOpEx:
                response.Message = invOpEx.Message;
                context.Response.StatusCode = response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case UnauthorizedAccessException unAuthEx:
                response.Message = unAuthEx.Message;
                context.Response.StatusCode = response.StatusCode = (int)HttpStatusCode.Unauthorized;
                break;

            case SecurityTokenExpiredException:
                response.Message = "Token expired.";
                context.Response.StatusCode = response.StatusCode = (int)HttpStatusCode.Unauthorized;
                break;

            case KeyNotFoundException keyNotFoundEx:
                response.Message = keyNotFoundEx.Message;
                context.Response.StatusCode = response.StatusCode = (int)HttpStatusCode.NotFound;
                break;

            case DbUpdateException:
                response.Message = "A persistence error occurred.";
                context.Response.StatusCode = response.StatusCode = (int)HttpStatusCode.Conflict;
                break;

            default:
                response.Message = "An error occurred while processing your request.";
                context.Response.StatusCode = response.StatusCode = (int)HttpStatusCode.InternalServerError;
                break;
        }

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

public sealed class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? TraceId { get; set; }
}