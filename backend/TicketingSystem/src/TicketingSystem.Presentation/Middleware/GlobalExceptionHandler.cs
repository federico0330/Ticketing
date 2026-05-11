using System.Net;
using System.Text.Json;
using TicketingSystem.Domain.Exceptions;

namespace TicketingSystem.Presentation.Middleware;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CODE-ERROR] - An unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new { Error = "INTERNAL_ERROR", Message = "An unexpected error occurred." };

        switch (exception)
        {
            case SeatNotAvailableException:
            case ConcurrencyException:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                response = new { Error = "CONFLICT", Message = exception.Message };
                break;
            case SeatNotFoundException:
            case EventNotFoundException:
            case ReservationNotFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response = new { Error = "NOT_FOUND", Message = exception.Message };
                break;
            case PaymentFailedException:
            case ReservationExpiredException:
            case InvalidOperationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new { Error = "BAD_REQUEST", Message = exception.Message };
                break;
            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                break;
        }

        var result = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(result);
    }
}
