using System.Text.Json;
using GreenAge.Core;
using Microsoft.AspNetCore.Mvc;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (DomainException exception)
        {
            context.Response.StatusCode = exception.StatusCode;
            context.Response.ContentType = "application/problem+json";
            var problem = new ProblemDetails
            {
                Status = exception.StatusCode,
                Title = exception.StatusCode switch { 400 => "Bad Request", 401 => "Unauthorized", 403 => "Forbidden", 404 => "Not Found", 409 => "Conflict", _ => "Request Failed" },
                Detail = exception.Message,
                Instance = context.Request.Path
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled API exception");
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new ProblemDetails { Status = 500, Title = "Internal Server Error" }));
        }
    }
}