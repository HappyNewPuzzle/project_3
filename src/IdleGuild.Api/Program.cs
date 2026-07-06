using System.IdentityModel.Tokens.Jwt;
using IdleGuild.Api.Endpoints;
using IdleGuild.Api.OpenApi;
using IdleGuild.Application;
using IdleGuild.Infrastructure;
using IdleGuild.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// 로컬·컨테이너·CI에서 같은 방식으로 수집하도록 Console과 Debug 로그만 사용합니다.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// API 문서, 상태 확인, 테스트 가능한 서버 시각을 의존성 컨테이너에 등록합니다.
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<
        BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<
        BearerSecurityRequirementTransformer>();
});
builder.Services.AddHealthChecks();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddApplication();

// 실제 비밀값은 환경 변수나 User Secrets가 appsettings의 자리표시자를 덮어씁니다.
var gameDatabaseConnection = builder.Configuration
    .GetConnectionString("GameDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'GameDatabase' is required.");
var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "Configuration section 'Jwt' is required.");

if (!builder.Environment.IsDevelopment() &&
    jwtOptions.SigningKey.StartsWith(
        "CHANGE_ME",
        StringComparison.Ordinal))
{
    throw new InvalidOperationException(
        "A non-placeholder JWT signing key is required outside Development.");
}

builder.Services.AddInfrastructure(
    gameDatabaseConnection,
    jwtOptions);
builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters =
            JwtTokenValidation.Create(jwtOptions);
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var subject = context.Principal?
                    .FindFirst(JwtRegisteredClaimNames.Sub)?
                    .Value;

                if (!Guid.TryParse(subject, out _))
                {
                    context.Fail(
                        "Token subject must be a player ID.");
                }

                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// 내부 API 정보 노출을 줄이기 위해 개발 환경에서만 문서 UI를 제공합니다.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Idle Guild API v1");
    });
}

// 인증 미들웨어가 JWT를 검증한 뒤 Endpoint의 권한 정책을 평가합니다.
app.UseAuthentication();
app.UseAuthorization();

// 시스템, 계정, 상태, 보상, 영웅, 스테이지 Endpoint를 각각 연결합니다.
app.MapHealthChecks("/health");
app.MapSystemEndpoints();
app.MapAccountEndpoints();
app.MapGameStateEndpoints();
app.MapRewardsEndpoints();
app.MapHeroesEndpoints();
app.MapStagesEndpoints();

app.Run();

// WebApplicationFactory가 테스트에서 진입점 형식을 참조할 수 있게 공개합니다.
public partial class Program;
