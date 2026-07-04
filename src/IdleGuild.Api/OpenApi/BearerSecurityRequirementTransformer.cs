using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace IdleGuild.Api.OpenApi;

/// <summary>인증이 필요한 Endpoint에만 Bearer 요구사항을 표시합니다.</summary>
public sealed class BearerSecurityRequirementTransformer :
    IOpenApiOperationTransformer
{
    /// <summary>Authorize 메타데이터가 있는 OpenAPI Operation에 보안 요구를 추가합니다.</summary>
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var endpointMetadata =
            context.Description.ActionDescriptor.EndpointMetadata;
        var requiresAuthorization = endpointMetadata
            .OfType<IAuthorizeData>()
            .Any();
        var allowsAnonymous = endpointMetadata
            .OfType<IAllowAnonymous>()
            .Any();

        if (!requiresAuthorization ||
            allowsAnonymous ||
            context.Document is null)
        {
            return Task.CompletedTask;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(
                "Bearer",
                context.Document)] = []
        });

        return Task.CompletedTask;
    }
}
