using System.Net;
using System.Text.Json;
namespace TaskFlow.Api.Middleware;
public class ExceptionHandlingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception ex)
        {
            var status = ex switch { ArgumentException => HttpStatusCode.BadRequest, UnauthorizedAccessException => HttpStatusCode.Forbidden, _ => HttpStatusCode.InternalServerError };
            context.Response.StatusCode = (int)status; context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
        }
    }
}
