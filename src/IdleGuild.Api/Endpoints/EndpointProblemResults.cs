using IdleGuild.Domain.Requests;
using Microsoft.Extensions.Primitives;

namespace IdleGuild.Api.Endpoints;

/// <summary>Endpoint에서 반복되는 오류 응답과 멱등 키 검증을 한곳에 모읍니다.</summary>
internal static class EndpointProblemResults
{
    public const string IdempotencyKeyHeaderName = "Idempotency-Key";

    public static IResult BadRequest(
        string title,
        string detail) =>
        TypedResults.Problem(
            title: title,
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest);

    public static IResult NotFound(
        string title,
        string detail) =>
        TypedResults.Problem(
            title: title,
            detail: detail,
            statusCode: StatusCodes.Status404NotFound);

    public static IResult Conflict(
        string title,
        string detail) =>
        TypedResults.Problem(
            title: title,
            detail: detail,
            statusCode: StatusCodes.Status409Conflict);

    public static IResult ServiceUnavailable(
        string title,
        string detail) =>
        TypedResults.Problem(
            title: title,
            detail: detail,
            statusCode:
                StatusCodes.Status503ServiceUnavailable);

    public static bool TryReadIdempotencyKey(
        HttpRequest request,
        out string idempotencyKey,
        out IResult? problem)
    {
        idempotencyKey = string.Empty;
        problem = null;

        if (!request.Headers.TryGetValue(
                IdempotencyKeyHeaderName,
                out StringValues headerValue) ||
            headerValue.Count != 1 ||
            string.IsNullOrWhiteSpace(headerValue[0]))
        {
            problem = BadRequest(
                "Idempotency key is required.",
                $"{IdempotencyKeyHeaderName} header is required.");
            return false;
        }

        idempotencyKey = headerValue[0]!.Trim();

        if (idempotencyKey.Length >
            IdempotencyPolicy.MaxKeyLength)
        {
            problem = BadRequest(
                "Idempotency key is too long.",
                $"{IdempotencyKeyHeaderName} cannot exceed {IdempotencyPolicy.MaxKeyLength} characters.");
            return false;
        }

        return true;
    }
}
