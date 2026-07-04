using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace IdleGuild.Api.OpenApi;

/// <summary>Swagger UI에 JWT Bearer 입력 방식을 등록합니다.</summary>
public sealed class BearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider) :
    IOpenApiDocumentTransformer
{
    /// <summary>Bearer 인증이 활성화된 경우 OpenAPI 보안 스키마를 추가합니다.</summary>
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider
            .GetAllSchemesAsync();

        if (authenticationSchemes.All(scheme =>
                scheme.Name != "Bearer"))
        {
            return;
        }

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes =
            new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    In = ParameterLocation.Header,
                    BearerFormat = "JWT"
                }
            };
    }
}
