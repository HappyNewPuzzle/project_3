# Idle Guild

서버 권위형 방치형 게임의 구조를 학습하고, 설계 과정과 테스트 근거를 함께 보여 주기 위한 포트폴리오 프로젝트입니다.

플레이어는 모험가 길드를 운영합니다. 영웅은 시간이 흐르는 동안 자원을 생산하고 스테이지를 진행하며, 플레이어는 획득한 골드로 영웅을 성장시킵니다.

> Portfolio MVP · Unity 2D Client + ASP.NET Core API + PostgreSQL

## 플레이 데모

```text
자동 전투 → 골드 획득 → 영웅 성장 → 액티브 스킬
→ 장비 드롭·자동 장착 → 보스 처치 → 지역 해금 → 서버 저장·복원
```

Unity 클라이언트는 소녀, 빨간 리본을 단 검은 고양이, 클래식 영웅을 선택할 수 있습니다. 산길을 달리며 일반 몬스터와 전투하고 7번째 전투마다 보스를 만납니다. 포트폴리오 데모 밸런스에서는 약 2분 동안 숲·동굴·설원 지역 전환을 확인할 수 있습니다.

## 핵심 구현

- 캐릭터별 Idle/Run/Attack/Hit Animator와 발 위치 보정
- 배경 스크롤, 자동 전투, 적 공격, 보스 패턴과 제한시간
- 타격 이펙트, 화면 흔들림, 데미지 숫자, 스킬 전용 VFX
- 성장, 치명타, 공격속도, 장비 드롭·자동 장착, 스킬 강화
- 게스트 JWT, 서버 권위 재화·스테이지 판정, 방치 보상
- PostgreSQL 저장, EF Core Migration, 멱등성, 동시성 방어
- 선택 영웅과 성장 상태의 API 저장 및 재접속 복원
- Mock API와 실제 Docker API 모드 분리

## 아키텍처

```text
Unity 6000.3 Client
  ├─ GameWorld: 전투·애니메이션·VFX
  ├─ IdleHud: 성장·장비·스킬·임무 UI
  └─ ApiClient: JWT + REST + 재접속
              │
              ▼
ASP.NET Core API
  ├─ Domain: 보상·성장·스테이지 규칙
  ├─ Application: 유스케이스·멱등 처리
  └─ Infrastructure: EF Core Repository
              │
              ▼
PostgreSQL 18
```

## 목표

- 서버가 시간, 재화, 성장 결과를 검증하는 구조 구현
- 오프라인 보상과 중복 요청을 안전하게 처리
- 작은 기능을 테스트 가능한 단위로 완성
- Unity 클라이언트가 사용할 명확한 API 계약 제공

## 기술 구성

- Client: Unity 6000.3 / C# / Universal 2D
- Server: ASP.NET Core Web API
- Database: PostgreSQL
- ORM: Entity Framework Core
- Local environment: Docker Compose
- API documentation: OpenAPI (Swagger)
- Tests: xUnit 기반 단위 테스트 및 통합 테스트

## 문서

- [게임 설계](Docs/GAME_DESIGN.md)
- [서버 아키텍처](Docs/ARCHITECTURE.md)
- [IdleGuild.Api 폴더와 파일 구조](Docs/API_STRUCTURE.md)
- [Step별 진행 이유와 의사결정](Docs/STEP_DECISIONS.md)
- [서버 MVP 완료 정리](Docs/SERVER_MVP_COMPLETE.md)
- [서버 고도화 로드맵](Docs/SERVER_HARDENING_ROADMAP.md)
- [장비 시스템](Docs/EQUIPMENT_SYSTEM.md)
- [Redis 도입 의사결정](Docs/REDIS_DECISION.md)
- [모의 상점과 구매 이력](Docs/MOCK_SHOP.md)
- [Unity 클라이언트 서버 연동](Docs/UNITY_CLIENT_INTEGRATION.md)
- [골드 변경 이력 원장](Docs/GOLD_LEDGER.md)
- [API 요청 속도 제한](Docs/RATE_LIMITING.md)
- [관리자 조회 API](Docs/ADMIN_API.md)
- [데모 시나리오](Docs/DEMO_SCENARIO.md)
- [배포와 운영 설정](Docs/DEPLOYMENT.md)
- [API 컨테이너 빌드와 실행](Docs/CONTAINER_DEPLOYMENT.md)
- [Liveness와 Readiness Health Check](Docs/HEALTH_CHECKS.md)
- [데이터베이스](Docs/DATABASE.md)
- [게스트 인증](Docs/AUTHENTICATION.md)
- [방치 보상](Docs/IDLE_REWARDS.md)
- [선택 영웅 저장과 방치 보상 미리보기](Docs/PLAYER_PROFILE_AND_IDLE_PREVIEW.md)
- [영웅 강화](Docs/HERO_UPGRADES.md)
- [스테이지 진행](Docs/STAGE_PROGRESSION.md)
- [API 오류 계약](Docs/API_ERRORS.md)
- [로깅과 예외 처리](Docs/OBSERVABILITY.md)
- [프로젝트 폴더 및 파일 구조](Docs/PROJECT_STRUCTURE.md)
- [개발 로드맵](Docs/ROADMAP.md)

## 현재 상태

포트폴리오 MVP가 완성되었습니다. Unity 2D 자동 전투 화면, 세 영웅, 지역별 몬스터, 보스, 성장·장비·스킬 HUD와 ASP.NET Core/PostgreSQL 저장·복원 흐름이 연결되어 있습니다. Docker 환경에서 신규 게스트 생성, 성장·장비·스킬·선택 영웅 저장, API 재시작 후 복원을 검증했습니다.

이 프로젝트는 스토어 출시 제품이 아니라 Unity와 서버 권위형 게임 구조를 학습하고 설명하기 위한 포트폴리오 범위입니다. 결제, 광고, 소셜 로그인, PvP는 의도적으로 제외했습니다.

## Unity 클라이언트 실행

1. Unity Hub에서 `UnityClient/My project`를 Unity `6000.3.19f1`로 엽니다.
2. `Assets/IdleGuild/Scenes/MainScene.unity`를 엽니다.
3. Play를 누르면 기본 Mock API 모드로 즉시 자동 전투를 확인할 수 있습니다.
4. 실제 서버 빌드는 `IDLE_GUILD_SERVER_BUILD` define을 사용합니다.

Android 테스트 빌드 메뉴:

```text
Idle Guild > Build Android > Development APK
Idle Guild > Build Android > Optimized Test APK
```

출력 위치:

```text
UnityClient/My project/Builds/Android/
```

## 로컬 실행

```powershell
dotnet restore
dotnet tool restore

Copy-Item .env.example .env
docker compose up -d --wait

$env:ConnectionStrings__GameDatabase = "Host=localhost;Port=5432;Database=idleguild;Username=idleguild;Password=replace_with_local_password"
$env:Jwt__SigningKey = "replace_with_a_random_secret_of_at_least_32_bytes"
dotnet tool run dotnet-ef database update --project src/IdleGuild.Infrastructure --startup-project src/IdleGuild.Infrastructure

dotnet run --project src/IdleGuild.Api
```

`dotnet run`을 실행한 PowerShell 창은 서버 프로세스가 실행 중인 창이므로 닫지 않습니다. 서버가 켜지면 대략 다음과 같은 로그가 표시됩니다.

```text
Now listening on: http://localhost:5219
Application started. Press Ctrl+C to shut down.
```

`.env.example`의 비밀번호는 로컬 개발 전용이며 운영 환경에서는 반드시 별도의 비밀값으로 교체해야 합니다.

서버 실행 후 새 PowerShell 창에서 다음 명령으로 상태를 확인할 수 있습니다.

```powershell
Invoke-RestMethod http://localhost:5219/health
Invoke-RestMethod http://localhost:5219/ready
Invoke-RestMethod http://localhost:5219/api/v1/system/status
```

브라우저나 HTTP Client에서는 다음 주소를 확인할 수 있습니다. URL을 PowerShell에 그대로 입력하면 명령어로 해석되어 오류가 나므로, 브라우저 주소창에 입력하거나 `Invoke-RestMethod`를 사용합니다.

- Health Check: `http://localhost:5219/health`
- Readiness Check: `http://localhost:5219/ready`
- System Status: `http://localhost:5219/api/v1/system/status`
- Create Guest: `POST http://localhost:5219/api/v1/accounts/guest`
- Game State: `GET http://localhost:5219/api/v1/game-state`
- Idle Reward: `POST http://localhost:5219/api/v1/rewards/idle/claim`
- Main Hero Upgrade: `POST http://localhost:5219/api/v1/heroes/main/upgrade`
- Stage Challenge: `POST http://localhost:5219/api/v1/stages/{stage}/challenge`
- Admin Player: `GET http://localhost:5219/api/v1/admin/players/{playerId}`
- Admin Gold Ledger: `GET http://localhost:5219/api/v1/admin/players/{playerId}/gold-ledger`
- Swagger UI: `http://localhost:5219/swagger`

## 빠른 데모 흐름

서버 실행 후 [데모 시나리오](Docs/DEMO_SCENARIO.md)를 따라가면 다음 핵심 루프를 확인할 수 있습니다.

```text
게스트 생성
→ 게임 상태 조회
→ 방치 보상 수령
→ 영웅 강화
→ 스테이지 2 도전
→ 생산 보너스 5% 반영 확인
```

IDE의 HTTP Client를 사용한다면 [IdleGuild.Api.http](src/IdleGuild.Api/IdleGuild.Api.http) 파일에서 같은 흐름을 순서대로 호출할 수 있습니다.

전체 테스트는 다음 명령으로 실행합니다.

```powershell
dotnet test
```

PostgreSQL 통합 테스트는 기본적으로 Testcontainers가 일회용 DB를 자동 생성하므로 Docker Desktop이 실행 중이어야 합니다.

API와 PostgreSQL을 모두 컨테이너로 실행하는 순서는 [API 컨테이너 빌드와 실행](Docs/CONTAINER_DEPLOYMENT.md)을 참고합니다.

## 서버 MVP 범위

현재 서버 MVP의 완료 기준, 포함 기능, 제외 범위, 다음 확장 후보는 [서버 MVP 완료 정리](Docs/SERVER_MVP_COMPLETE.md)에 정리되어 있습니다.

운영 게임 서버 수준으로 확장하기 위한 다음 작업 목록은 [서버 고도화 로드맵](Docs/SERVER_HARDENING_ROADMAP.md)에 정리되어 있습니다.
