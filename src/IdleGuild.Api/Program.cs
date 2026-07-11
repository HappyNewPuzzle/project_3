using System.IdentityModel.Tokens.Jwt;
using IdleGuild.Api.Endpoints;
using IdleGuild.Api.ErrorHandling;
using IdleGuild.Api.HealthChecks;
using IdleGuild.Api.OpenApi;
using IdleGuild.Api.RateLimiting;
using IdleGuild.Api.Authorization;
using IdleGuild.Api.Observability;
using IdleGuild.Application;
using IdleGuild.Infrastructure;
using IdleGuild.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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
builder.Services
    .AddHealthChecks()
    .AddCheck<PostgreSqlReadinessHealthCheck>(
        "postgresql",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"],
        timeout: TimeSpan.FromSeconds(3));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddApiRateLimiting();
builder.Services.AddHttpLogging(options =>
{
    // 운영 로그에 필요한 최소 HTTP 정보만 남겨 토큰과 Body 노출을 피합니다.
    options.LoggingFields =
        HttpLoggingFields.RequestMethod |
        HttpLoggingFields.RequestPath |
        HttpLoggingFields.ResponseStatusCode |
        HttpLoggingFields.Duration;
});
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
builder.Services.AddAuthorization(options =>
{
    // 관리자 API는 서명된 JWT 중 명시적인 admin 계정 유형만 허용합니다.
    options.AddPolicy(
        AdminAuthorization.PolicyName,
        policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim(
                AdminAuthorization.AccountTypeClaim,
                AdminAuthorization.AdminAccountType));
});

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

// 예외 처리까지 포함한 최종 상태와 시간을 측정하고 클라이언트에 Trace ID를 반환합니다.
app.UseMiddleware<ApiObservabilityMiddleware>();

// 처리되지 않은 예외를 500 ProblemDetails로 통일하고 추적 가능한 로그를 남깁니다.
app.UseExceptionHandler();

// 요청 메서드, 경로, 상태 코드, 소요 시간을 남겨 API 동작을 추적합니다.
app.UseHttpLogging();

// 인증 미들웨어가 JWT를 검증한 뒤 Endpoint의 권한 정책을 평가합니다.
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

// 생존 확인은 외부 의존성을 제외하고 준비 확인에서만 PostgreSQL을 검사합니다.
app.MapHealthChecks(
    "/health",
    new HealthCheckOptions
    {
        Predicate = _ => false
    });
app.MapHealthChecks(
    "/ready",
    new HealthCheckOptions
    {
        Predicate = registration =>
            registration.Tags.Contains("ready")
    });
app.MapSystemEndpoints();
app.MapAccountEndpoints();
app.MapGameStateEndpoints();
app.MapRewardsEndpoints();
app.MapHeroesEndpoints();
app.MapStagesEndpoints();
app.MapAdminEndpoints();
app.MapEquipmentEndpoints();
app.MapShopEndpoints();

app.Run();

// WebApplicationFactory가 테스트에서 진입점 형식을 참조할 수 있게 공개합니다.
public partial class Program;
