using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace TicketingSystem.Presentation.Middlewares;

public class GlobalHeaderMiddleware
{
    private readonly RequestDelegate _next;

    public GlobalHeaderMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey("X-Api-version"))
            {
                context.Response.Headers["X-Api-version"] = "1.0";
            }
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
