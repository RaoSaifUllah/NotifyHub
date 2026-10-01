using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace NotifyHub.Api.Operations;
public sealed class SafeExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, code) = exception switch
        {
            ArgumentException => (400, "The request contains invalid values.", "VALIDATION_FAILED"),
            UnauthorizedAccessException => (403, "Permission is required.", "FORBIDDEN"),
            KeyNotFoundException => (404, "The resource was not found.", "RESOURCE_NOT_FOUND"),
            DbUpdateConcurrencyException => (409, "The resource changed. Reload and try again.", "CONCURRENT_CHANGE"),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } => (409, "The request conflicts with an existing resource.", "RESOURCE_CONFLICT"),
            NpgsqlException => (503, "The database is unavailable.", "DEPENDENCY_UNAVAILABLE"),
            _ => (500, "The request could not be completed.", "INTERNAL_ERROR")
        };
        context.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails {
                Status = status, Title = title,
                Extensions = { ["code"] = code, ["correlationId"] = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier }
            }
        });
    }
}
