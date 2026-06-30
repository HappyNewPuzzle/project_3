# Idle Guild

서버 권위형 방치형 게임의 구조를 학습하고, 설계 과정과 테스트 근거를 함께 보여 주기 위한 포트폴리오 프로젝트입니다.

플레이어는 모험가 길드를 운영합니다. 영웅은 시간이 흐르는 동안 자원을 생산하고 스테이지를 진행하며, 플레이어는 획득한 골드로 영웅을 성장시킵니다.

## 목표

- 서버가 시간, 재화, 성장 결과를 검증하는 구조 구현
- 오프라인 보상과 중복 요청을 안전하게 처리
- 작은 기능을 테스트 가능한 단위로 완성
- Unity 클라이언트가 사용할 명확한 API 계약 제공

## 기술 구성

- Client: Unity / C# (서버 MVP 이후 별도 진행)
- Server: ASP.NET Core Web API
- Database: PostgreSQL
- ORM: Entity Framework Core
- Local environment: Docker Compose
- API documentation: OpenAPI (Swagger)
- Tests: xUnit 기반 단위 테스트 및 통합 테스트

## 문서

- [게임 설계](Docs/GAME_DESIGN.md)
- [서버 아키텍처](Docs/ARCHITECTURE.md)
- [데이터베이스](Docs/DATABASE.md)
- [개발 로드맵](Docs/ROADMAP.md)

## 현재 상태

Step 3: PostgreSQL, EF Core Migration, 게임 상태 모델과 실제 DB 통합 테스트를 구성했습니다.

## 로컬 실행

```powershell
dotnet restore
dotnet tool restore

Copy-Item .env.example .env
docker compose up -d --wait

$env:ConnectionStrings__GameDatabase = "Host=localhost;Port=5432;Database=idleguild;Username=idleguild;Password=replace_with_local_password"
dotnet tool run dotnet-ef database update --project src/IdleGuild.Infrastructure --startup-project src/IdleGuild.Infrastructure

dotnet run --project src/IdleGuild.Api
```

`.env.example`의 비밀번호는 로컬 개발 전용이며 운영 환경에서는 반드시 별도의 비밀값으로 교체해야 합니다.

실행 후 다음 주소를 확인할 수 있습니다.

- Health Check: `http://localhost:5219/health`
- System Status: `http://localhost:5219/api/v1/system/status`
- Swagger UI: `http://localhost:5219/swagger`

전체 테스트는 다음 명령으로 실행합니다.

```powershell
dotnet test
```

PostgreSQL 통합 테스트는 기본적으로 Testcontainers가 일회용 DB를 자동 생성하므로 Docker Desktop이 실행 중이어야 합니다.
