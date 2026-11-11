using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Exceptions;

namespace SchoolAdmission.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _log;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> log)
    {
        _next = next;
        _log = log;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (AppException ex)
        {
            await Write(ctx, ex.StatusCode, ex.Code, ex.Message);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await Write(ctx, 409, "CONFLICT", "This request conflicts with existing data.");
        }
        catch (BadHttpRequestException ex)
        {
            await Write(ctx, 400, "INVALID_REQUEST", ex.Message);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Unhandled exception");
            await Write(ctx, 500, "INTERNAL_ERROR", "An unexpected error occurred. Please try again.");
        }
    }

    private static Task Write(HttpContext ctx, int status, string code, string message)
    {
        if (ctx.Response.HasStarted) return Task.CompletedTask;
        ctx.Response.Clear();
        ctx.Response.StatusCode = status;
        return ctx.Response.WriteAsJsonAsync(new ErrorResponse(code, message));
    }
}