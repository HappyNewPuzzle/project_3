using IdleGuild.Api.Endpoints;
using IdleGuild.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// API 문서, 상태 확인, 테스트 가능한 서버 시각을 의존성 컨테이너에 등록합니다.
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton(TimeProvider.System);

// 실제 비밀값은 환경 변수나 User Secrets가 appsettings의 자리표시자를 덮어씁니다.
var gameDatabaseConnection = builder.Configuration
    .GetConnectionString("GameDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'GameDatabase' is required.");
builder.Services.AddInfrastructure(gameDatabaseConnection);

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

// 인프라 상태와 클라이언트용 시스템 상태 엔드포인트를 각각 연결합니다.
app.MapHealthChecks("/health");
app.MapSystemEndpoints();

app.Run();

// WebApplicationFactory가 테스트에서 진입점 형식을 참조할 수 있게 공개합니다.
public partial class Program;
