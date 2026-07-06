using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace IdleGuild.Api.ErrorHandling;

/// <summary>처리되지 않은 예외를 로그로 남기고 표준 ProblemDetails 응답으로 변환합니다.</summary>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment,
    IProblemDetailsService problemDetailsService) :
    IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ??
            httpContext.TraceIdentifier;

        // 예외 원인은 서버 로그에 남기고 클라이언트에는 추적 ID 중심의 안전한 응답만 제공합니다.
        logger.LogError(
            exception,
            "Unhandled exception occurred. TraceId: {TraceId}",
            traceId);

        httpContext.Response.StatusCode =
            StatusCodes.Status500InternalServerError;

        var problemDetails = new ProblemDetails
        {
            Title = "An unexpected server error occurred.",
            Detail = environment.IsDevelopment()
                ? exception.Message
                : "Contact support with the traceId if the problem persists.",
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://httpstatuses.com/500"
        };
        problemDetails.Extensions["traceId"] = traceId;

        return await problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = problemDetails
            });
    }
}
