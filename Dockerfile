# syntax=docker/dockerfile:1

# SDK 이미지는 복원과 게시 과정에만 사용하고 최종 이미지에는 포함하지 않습니다.
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /source

# 프로젝트 파일을 먼저 복사해 소스 변경 시에도 NuGet 복원 레이어를 재사용합니다.
COPY Directory.Build.props global.json ./
COPY src/IdleGuild.Api/IdleGuild.Api.csproj src/IdleGuild.Api/
COPY src/IdleGuild.Application/IdleGuild.Application.csproj src/IdleGuild.Application/
COPY src/IdleGuild.Domain/IdleGuild.Domain.csproj src/IdleGuild.Domain/
COPY src/IdleGuild.Infrastructure/IdleGuild.Infrastructure.csproj src/IdleGuild.Infrastructure/
RUN dotnet restore src/IdleGuild.Api/IdleGuild.Api.csproj

# 복원 후 실제 서버 소스만 복사해 Release 게시 결과를 만듭니다.
COPY src/ src/
RUN dotnet publish src/IdleGuild.Api/IdleGuild.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ASP.NET Runtime과 게시 결과만 남겨 이미지 크기와 공격 표면을 줄입니다.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080

# 컨테이너 건강 상태는 API뿐 아니라 PostgreSQL까지 준비됐을 때만 healthy가 됩니다.
HEALTHCHECK --interval=10s --timeout=3s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://127.0.0.1:8080/ready || exit 1

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish ./

# .NET Runtime 이미지의 비루트 기본 계정으로 서버를 실행합니다.
USER $APP_UID
ENTRYPOINT ["dotnet", "IdleGuild.Api.dll"]
