# 프로젝트 폴더 및 파일 구조

이 문서는 `project3` 저장소에 있는 폴더와 파일의 역할, 생성 이유, 수정 시점을 설명합니다. 현재 프로젝트는 ASP.NET Core·PostgreSQL 서버 고도화 Step 1~9와 Unity 개발용 클라이언트 API 연동까지 구현되어 있습니다. 뒤쪽의 Step별 추가 설명은 당시 변경 이유를 보존한 기록이며, 현재 상태는 이 문서 상단과 기능별 전용 문서를 우선합니다.

## 1. 가장 먼저 이해할 구조

```text
project3
├─ Docs/                          설계와 학습 문서
├─ src/                           배포되는 실제 서버 코드
│  ├─ IdleGuild.Api/              HTTP 요청과 서버 실행
│  ├─ IdleGuild.Application/      유스케이스 조정
│  ├─ IdleGuild.Domain/           순수 게임 규칙과 상태
│  └─ IdleGuild.Infrastructure/   PostgreSQL과 외부 기술 구현
├─ tests/                         자동 검증 코드
│  ├─ IdleGuild.Api.Tests/
│  ├─ IdleGuild.Application.Tests/
│  ├─ IdleGuild.Domain.Tests/
│  └─ IdleGuild.Infrastructure.Tests/
├─ UnityClient/My project/        Unity 6000.3 Universal 2D 클라이언트
├─ compose.yaml                   로컬 PostgreSQL 실행 환경
├─ compose.api.yaml               API 컨테이너 조합 설정
├─ Dockerfile                     API 운영 이미지 빌드
├─ Directory.Build.props          .NET 공통 빌드 규칙
├─ global.json                    사용할 .NET SDK 버전
├─ dotnet-tools.json              저장소 로컬 EF Core 도구
├─ IdleGuild.sln                  전체 .NET 프로젝트 묶음
└─ README.md                      프로젝트 시작 안내
```

프로덕션 코드의 의존성 방향은 다음과 같습니다.

```text
IdleGuild.Api
    ├────────────> IdleGuild.Application ────────> IdleGuild.Domain
    └────────────> IdleGuild.Infrastructure ─────> IdleGuild.Domain
                              └───────────────────> IdleGuild.Application
```

의존성은 바깥 계층에서 안쪽 계층으로 향합니다. 가장 안쪽의 `Domain`은 웹 서버, EF Core, PostgreSQL을 참조하지 않습니다. 이 규칙 덕분에 게임 규칙을 기술 구현과 분리해 빠르게 테스트할 수 있습니다.

## 2. 루트 폴더

### `.git/`

Git이 커밋, 브랜치, 원격 저장소 정보를 보관하는 내부 폴더입니다.

- Git 명령이 자동으로 관리합니다.
- 애플리케이션 코드가 아니며 직접 수정하지 않습니다.
- 삭제하면 로컬 저장소의 Git 이력을 인식할 수 없게 됩니다.

### `.agents/`

개발 보조 도구가 작업 정보를 관리하기 위해 사용할 수 있는 내부 폴더입니다.

- 게임 서버 실행과는 관계가 없습니다.
- 현재 애플리케이션 파일은 들어 있지 않습니다.
- 도구가 관리하므로 직접 코드를 추가하지 않습니다.

### `Docs/`

프로젝트의 의도와 결정 사항을 기록하는 폴더입니다. 코드는 현재 동작을 보여 주고, 문서는 코드가 그렇게 만들어진 이유를 설명합니다.

새 기능을 만들 때 관련 규칙이나 구조가 달라졌다면 코드와 함께 이 폴더의 문서도 수정합니다.

### `src/`

실제 서버를 구성하는 프로덕션 코드가 들어갑니다. 테스트 전용 코드는 이곳에 넣지 않습니다.

`Api`, `Application`, `Domain`, `Infrastructure`로 책임을 나눠 특정 프레임워크나 DB 기술이 게임 규칙 전체로 퍼지는 것을 막습니다.

### `tests/`

프로덕션 코드의 동작과 구조를 자동으로 검증합니다.

- `Domain.Tests`: 순수 게임 규칙을 빠르게 검증합니다.
- `Application.Tests`: 유스케이스 처리 순서를 검증합니다.
- `Api.Tests`: HTTP 요청과 응답을 검증합니다.
- `Infrastructure.Tests`: 실제 PostgreSQL 저장 동작을 검증합니다.

프로덕션 프로젝트와 테스트 프로젝트를 분리한 이유는 테스트 패키지와 테스트용 컨테이너 코드가 실제 서버 배포물에 포함되지 않게 하기 위해서입니다.

### 자동 생성되는 `bin/`과 `obj/`

각 `.NET` 프로젝트 아래에는 빌드 후 `bin/`, `obj/` 폴더가 생깁니다.

- `bin/`: DLL, 실행 파일, 런타임 설정 등 최종 빌드 결과를 보관합니다.
- `obj/`: 컴파일 중간 파일, NuGet 복원 정보, 생성 코드를 보관합니다.
- `dotnet build`와 `dotnet test`가 자동으로 다시 생성합니다.
- 직접 수정하거나 Git에 커밋하지 않습니다.

## 3. 루트 파일

### `README.md`

저장소의 첫 화면이며 프로젝트에 들어온 사람이 가장 먼저 읽는 안내서입니다.

- 프로젝트 목적과 기술 구성을 설명합니다.
- 문서 목록을 연결합니다.
- PostgreSQL, Migration, API 실행 명령을 제공합니다.
- 현재 완료된 Step을 표시합니다.

실행 방법이나 주요 기술 구성이 바뀌면 반드시 수정합니다.

### `IdleGuild.sln`

8개의 `.NET` 프로젝트를 하나의 솔루션으로 묶습니다.

- Visual Studio의 Solution Explorer에 프로젝트를 표시합니다.
- `dotnet build IdleGuild.sln`으로 전체 프로젝트를 빌드할 수 있게 합니다.
- `dotnet test IdleGuild.sln`으로 모든 테스트 프로젝트를 실행할 수 있게 합니다.
- `src`와 `tests` 솔루션 폴더로 프로젝트를 보기 좋게 분류합니다.

프로젝트를 추가하거나 제거할 때 `dotnet sln add/remove` 명령으로 갱신합니다. 일반적으로 텍스트를 직접 편집하지 않습니다.

### `global.json`

프로젝트가 사용할 .NET SDK 버전을 `10.0.301`로 고정합니다.

개발자마다 다른 SDK를 사용하면 템플릿, 컴파일러, 빌드 결과가 달라질 수 있기 때문에 만들었습니다. SDK를 팀 전체에서 업그레이드할 때만 수정합니다.

### `Directory.Build.props`

모든 하위 `.csproj`에 공통으로 적용되는 빌드 규칙입니다.

- `ImplicitUsings`: 자주 쓰는 네임스페이스를 자동으로 가져옵니다.
- `Nullable`: null 가능성에 대한 컴파일 검사를 활성화합니다.
- `TreatWarningsAsErrors`: 컴파일 경고도 오류로 처리합니다.

각 프로젝트에 같은 설정을 반복하지 않고 품질 기준을 한곳에서 유지하기 위해 만들었습니다.

### `dotnet-tools.json`

저장소 전용 .NET CLI 도구 목록입니다.

현재 `dotnet-ef` 10.0.9를 고정합니다. 새 개발자는 `dotnet tool restore`만 실행하면 같은 EF Migration 도구를 사용할 수 있습니다.

도구 버전을 올리거나 새 로컬 도구를 추가할 때 수정되며, 보통 `dotnet tool install/update` 명령이 관리합니다.

### `compose.yaml`

로컬 PostgreSQL 18 컨테이너 실행 방법을 선언합니다.

- PostgreSQL 이미지와 포트를 지정합니다.
- DB 이름, 사용자, 비밀번호를 환경 변수로 받습니다.
- 데이터가 컨테이너 재시작 후에도 남도록 볼륨을 연결합니다.
- `pg_isready` Health Check로 DB 준비 상태를 확인합니다.

개발자가 PostgreSQL을 직접 설치하지 않아도 같은 버전과 설정으로 실행하게 하기 위해 만들었습니다.

### `compose.api.yaml`

기존 PostgreSQL Compose 구성에 Production API 컨테이너를 추가합니다. DB 준비 후 API를 시작하고 포트·JWT·연결 문자열을 환경 변수로 주입하며 비루트·읽기 전용 보안 설정을 적용합니다.

### `Dockerfile`

SDK 빌드 단계와 ASP.NET Runtime 실행 단계를 분리합니다. 최종 이미지에는 Release 게시 결과만 복사하고 .NET Runtime 이미지의 비루트 계정으로 `IdleGuild.Api.dll`을 실행합니다.

### `.dockerignore`

Docker Build Context에서 `.env`, Git, `bin/obj`, Docs, tests와 UnityClient를 제외합니다. 비밀값 유출과 불필요한 Context 전송, 캐시 무효화를 줄이기 위해 만들었습니다.

### `.env.example`

`compose.yaml`이 요구하는 환경 변수의 예시입니다.

- 실제 `.env`를 만들 때 복사할 템플릿입니다.
- 포함된 비밀번호는 로컬 개발용 자리표시자입니다.
- 실제 운영 비밀값을 넣지 않습니다.

환경 변수가 추가되면 이 파일에도 안전한 예시값을 추가합니다.

### `.gitignore`

Git이 추적하지 않아야 할 파일과 폴더를 정의합니다.

- `.NET`의 `bin/`, `obj/`
- Unity의 `Library/`, `Temp/`, `Build/`
- IDE의 `.vs/`, `.idea/`, `.vscode/`
- 실제 비밀값이 들어갈 `.env`
- 로컬 전용 `appsettings.Development.json`

빌드 결과와 비밀값이 원격 저장소에 올라가는 것을 막기 위해 필요합니다.

## 4. `Docs/` 내부

### `Docs/GAME_DESIGN.md`

게임 자체의 규칙을 정의합니다.

- 방치형 모험가 길드라는 콘셉트
- 골드 생산 → 보상 수령 → 강화 → 스테이지 진행의 핵심 루프
- MVP에 포함하거나 제외할 기능
- 초기 강화 비용과 전투력 공식
- 서버가 직접 검증해야 하는 값

게임 규칙이나 밸런스 공식이 바뀔 때 수정합니다.

### `Docs/ARCHITECTURE.md`

서버의 기술 구조와 계층별 책임을 정의합니다.

- 모듈형 모놀리스 선택 이유
- API, Application, Domain, Infrastructure의 책임
- 주요 모듈과 API 초안
- 데이터 저장 원칙과 품질 기준

새 계층이나 모듈이 추가되거나 의존성 방향이 바뀔 때 수정합니다.

### `Docs/DATABASE.md`

PostgreSQL과 EF Core 사용 방법을 설명합니다.

- `PlayerGameState`에서 PostgreSQL까지의 데이터 흐름
- `player_game_states` 테이블의 열
- Migration 생성·적용 명령
- `xmin` 동시성 토큰
- Testcontainers 통합 테스트 방식

테이블, Migration 방식, DB 실행 방법이 달라질 때 수정합니다.

### `Docs/AUTHENTICATION.md`

게스트 계정과 JWT 인증 경계를 설명합니다.

- 게스트 계정 생성 순서
- JWT Claim과 검증 항목
- 인증된 게임 상태 조회 흐름
- 서명 키 관리 방법
- 현재 MVP 인증의 제한사항

토큰 형식, 만료 정책, 인증 API가 달라질 때 수정합니다.

### `Docs/ROADMAP.md`

프로젝트를 작은 완료 단위로 나눈 개발 계획입니다.

- 각 Step에서 구현할 기능
- 완료 여부를 판단하는 조건
- 완료된 Step 상태
- Unity 클라이언트로 넘어갈 시점

Step을 시작하거나 완료할 때 상태를 갱신합니다.

### `Docs/STEP_DECISIONS.md`

각 Step에서 왜 그 작업을 진행했는지 정리하는 의사결정 문서입니다.

- Step별 목적
- 해결하려던 문제
- 선택한 구현 방향
- 검증 방법

포트폴리오 리뷰어가 구현 순서와 설계 의도를 빠르게 따라갈 수 있게 하기 위해 만들었습니다.

### `Docs/SERVER_MVP_COMPLETE.md`

서버 MVP가 어떤 기준으로 완료되었는지 정리합니다.

- 완료 선언
- 포함된 기능
- 핵심 플레이 루프
- 테스트 검증 기준
- 의도적으로 제외한 범위
- Unity 클라이언트로 넘어가기 전 확인할 것

서버 작업을 열린 TODO 목록이 아니라 완료된 포트폴리오 단위로 보여 주기 위해 만들었습니다.

### `Docs/DEMO_SCENARIO.md`

로컬 서버를 실행한 뒤 게스트 생성부터 스테이지 도전까지 핵심 게임 루프를 직접 확인하는 문서입니다.

- PowerShell 호출 예시
- 예상 응답에서 확인할 값
- 멱등 재시도 확인
- 오류 계약 확인

README를 너무 길게 만들지 않으면서도 포트폴리오 리뷰어가 실제 동작을 따라 해볼 수 있게 하기 위해 만들었습니다.

### `Docs/DEPLOYMENT.md`

운영 배포 전에 확인해야 할 설정과 체크리스트를 정리합니다.

- 필수 환경 변수
- Migration 적용 방법
- Development와 Production 동작 차이
- Health Check와 로그 기준
- 현재 MVP의 배포 제한사항

특정 클라우드에 종속되기 전에 공통 운영 기준을 먼저 명확히 하기 위해 만들었습니다.

### `Docs/PROJECT_STRUCTURE.md`

현재 읽고 있는 문서입니다.

프로젝트의 모든 주요 폴더와 파일을 어디서부터 읽어야 하는지 설명합니다. 파일이 추가·삭제되거나 폴더 책임이 달라질 때 함께 수정합니다.

### `Docs/ADMIN_API.md`

관리자 JWT Claim, 읽기 전용 상태·원장 Endpoint, 커서 페이지네이션과 운영 전 보안 확장 기준을 설명합니다.

### `Docs/CONTAINER_DEPLOYMENT.md`

다단계 이미지 빌드, Compose 실행, 별도 Migration, Health Check, 컨테이너 보안과 운영 이미지 태그 원칙을 설명합니다.

### `Docs/HEALTH_CHECKS.md`

API liveness와 PostgreSQL readiness의 차이, 상태 코드, Docker·Kubernetes probe 사용법과 장애 복구 기준을 설명합니다.

### `Docs/RATE_LIMITING.md`

게스트 생성과 인증된 상태 변경 API의 제한량, 분리 키, 429 오류 계약과 다중 서버 확장 조건을 설명합니다. Rate Limit 수치나 프록시·Redis 구성이 달라질 때 수정합니다.

### 현재 추가 문서

| 문서 | 역할 |
| --- | --- |
| `API_STRUCTURE.md` | 현재 `IdleGuild.Api`의 모든 폴더와 파일 설명 |
| `API_ERRORS.md` | ProblemDetails, 멱등 키와 HTTP 오류 계약 |
| `GOLD_LEDGER.md` | 골드 변경 감사 원장 규칙 |
| `HERO_UPGRADES.md` | 영웅 강화 공식과 멱등 처리 |
| `IDLE_REWARDS.md` | 방치 시간 계산과 보상 수령 |
| `PLAYER_PROFILE_AND_IDLE_PREVIEW.md` | 선택 영웅 저장, 읽기 전용 보상 미리보기와 Unity 계약 |
| `STAGE_PROGRESSION.md` | 스테이지 판정과 진행 저장 |
| `EQUIPMENT_SYSTEM.md` | 장비 마스터·보유·장착과 전투력 |
| `MOCK_SHOP.md` | 모의 상품 구매와 실제 결제의 경계 |
| `REDIS_DECISION.md` | Redis 도입 보류 이유와 재검토 기준 |
| `OBSERVABILITY.md` | Trace ID, 로그, 메트릭과 장애 조사 |
| `UNITY_CLIENT_INTEGRATION.md` | Unity 실행 순서와 최신 서버 연동 |

## 5. `src/IdleGuild.Api/`

HTTP 요청을 받아 Application 또는 Infrastructure에 전달하고 HTTP 응답으로 변환하는 프로젝트입니다. 게임 규칙을 직접 구현하는 장소가 아닙니다.

> 최신 기준: 이 절은 초기 MVP의 학습 기록을 포함합니다. 장비, 모의 상점, Health Check와 관측성까지 포함한 현재 전체 폴더·파일 설명은 [IdleGuild.Api 폴더와 파일 구조](API_STRUCTURE.md)를 기준으로 읽어주세요.

### `IdleGuild.Api.csproj`

API 프로젝트의 빌드 및 의존성 설정입니다.

- Web SDK를 사용해 ASP.NET Core 실행 파일을 만듭니다.
- 대상 프레임워크를 `net10.0`으로 지정합니다.
- OpenAPI 문서 생성 패키지를 참조합니다.
- 취약점이 수정된 `Microsoft.OpenApi` 2.x 버전을 직접 고정합니다.
- Swagger UI 패키지를 참조합니다.
- JWT Bearer 인증 패키지를 참조합니다.
- `Application`과 `Infrastructure` 프로젝트를 참조합니다.

API 전용 NuGet 패키지나 프로젝트 참조가 바뀔 때 수정합니다.

### `Program.cs`

서버 프로세스의 진입점이자 Composition Root입니다.

현재 다음 작업을 수행합니다.

1. ASP.NET Core 애플리케이션 Builder를 생성합니다.
2. Console·Debug 로그, OpenAPI, Health Check, `TimeProvider`를 등록합니다.
3. Application Handler를 DI 컨테이너에 등록합니다.
4. 설정에서 PostgreSQL 연결 문자열과 JWT 설정을 읽습니다.
5. Infrastructure의 DB 저장소와 토큰 발급기를 등록합니다.
6. JWT Bearer 서명, 발급자, 대상, 만료와 `sub`를 검증합니다.
7. Development 환경에서 OpenAPI와 Swagger UI를 활성화합니다.
8. 인증, Rate Limiting, 인가 미들웨어를 순서대로 연결합니다.
9. 시스템, 계정, 게임 상태, 관리자 Endpoint를 연결합니다.
10. 서버를 실행합니다.

`Program`을 `partial`로 공개한 이유는 `WebApplicationFactory<Program>`이 테스트에서 서버 진입점을 찾게 하기 위해서입니다.

새로운 큰 기능의 세부 구현을 이 파일에 직접 넣기보다, 각 계층의 등록 확장 메서드를 호출하는 형태로 유지합니다.

### `appsettings.json`

API의 기본 설정 파일입니다.

- PostgreSQL 연결 문자열 형식을 제공합니다.
- JWT 발급자, 대상, 서명 키 자리표시자와 만료시간을 제공합니다.
- 로그 수준을 지정합니다.
- 허용할 Host 설정을 제공합니다.

DB 비밀번호와 JWT 서명 키는 자리표시자일 뿐입니다. 실제 값은 환경 변수나 User Secrets로 덮어써야 합니다.

### `IdleGuild.Api.http`

IDE의 HTTP Client로 API를 직접 호출하는 예제 요청입니다.

- `/health`
- `/api/v1/system/status`
- `POST /api/v1/accounts/guest`
- Bearer 토큰을 사용하는 `GET /api/v1/game-state`

새 API를 만들면 수동 확인용 요청 예시를 추가할 수 있습니다.

### `Properties/`

API 프로젝트의 실행 프로필 설정을 모아 둡니다.

#### `Properties/launchSettings.json`

로컬 개발 실행 설정입니다.

- HTTP 실행 주소를 `http://localhost:5219`로 지정합니다.
- 환경을 `Development`로 설정합니다.
- 브라우저 자동 실행 여부를 지정합니다.

배포 서버 설정이 아니라 로컬 IDE와 `dotnet run` 편의를 위한 파일입니다.

### `Authentication/`

검증이 끝난 사용자의 Claims를 API가 안전하게 사용하는 보조 코드를 보관합니다.

#### `Authentication/ClaimsPrincipalExtensions.cs`

JWT의 `sub` Claim을 `Guid` 플레이어 ID로 변환합니다. Claim이 없거나 Guid 형식이 아니면 실패를 반환해 잘못된 사용자 식별자가 Handler로 전달되지 않게 합니다.

### `Authorization/`

인증된 JWT가 특정 운영 기능을 사용할 수 있는지 판단할 권한 정책 상수를 보관합니다.

#### `Authorization/AdminAuthorization.cs`

관리자 Policy 이름과 `account_type=admin` Claim 값을 정의합니다. 게스트 JWT와 운영자 JWT의 권한 경계를 한곳에서 공유하게 합니다.

### `OpenApi/`

JWT 인증 방식을 OpenAPI와 Swagger UI에 표시하는 Transformer를 보관합니다.

#### `OpenApi/BearerSecuritySchemeTransformer.cs`

등록된 인증 Scheme에 Bearer가 있으면 OpenAPI 문서의 `components.securitySchemes`에 HTTP Bearer JWT 방식을 추가합니다. Swagger UI의 Authorize 입력창을 만들기 위해 필요합니다.

#### `OpenApi/BearerSecurityRequirementTransformer.cs`

`RequireAuthorization` 메타데이터가 있는 Endpoint에만 Bearer 보안 요구사항을 추가합니다. 익명 API와 보호 API를 문서에서 구분할 수 있게 합니다.

### `RateLimiting/`

과도한 익명·인증 요청이 Application과 DB에 도달하기 전에 차단하는 API 운영 정책을 보관합니다.

#### `RateLimiting/ApiRateLimitPolicies.cs`

게스트 생성은 연결 IP, 게임 상태 변경은 JWT 플레이어 ID를 분리 키로 사용하는 1분 고정 윈도우를 등록합니다. 제한 초과 시 공통 `ProblemDetails` 서비스로 `Retry-After`가 포함된 429 응답을 작성합니다.

### `Contracts/`

HTTP 요청과 응답의 외부 계약 DTO를 보관합니다.

Domain 객체를 그대로 외부에 노출하면 내부 모델 변경이 API 파괴 변경으로 이어질 수 있으므로 별도 폴더로 분리했습니다.

#### `Contracts/SystemStatusResponse.cs`

시스템 상태 API가 반환하는 응답 형식입니다.

- `Status`: API 상태 문자열
- `ServerTimeUtc`: 서버가 판단한 현재 UTC 시각

Unity 클라이언트는 이 JSON 계약을 기준으로 응답을 역직렬화하게 됩니다.

#### `Contracts/GuestAccountResponse.cs`

게스트 생성 API가 반환하는 플레이어 ID, 액세스 토큰, 만료 UTC 시각을 정의합니다.

#### `Contracts/GameStateResponse.cs`

인증된 플레이어에게 반환할 골드, 영웅 레벨, 최고 스테이지, 마지막 방치 보상 정산 시각을 정의합니다. Domain 객체를 그대로 직렬화하지 않기 위해 별도 계약으로 만들었습니다.

### `Endpoints/`

Minimal API의 URL, HTTP 메서드, 입력과 출력 연결을 모아 둡니다.

기능별 Endpoint 파일로 나누면 `Program.cs`가 커지는 것을 막을 수 있습니다.

#### `Endpoints/SystemEndpoints.cs`

`GET /api/v1/system/status`를 등록합니다.

- `TimeProvider`를 DI로 받습니다.
- 현재 서버 UTC 시각을 조회합니다.
- `SystemStatusResponse`를 200 OK로 반환합니다.
- OpenAPI 이름, 설명, 응답 형식을 등록합니다.

`DateTimeOffset.UtcNow`를 직접 호출하지 않고 `TimeProvider`를 사용하는 이유는 이후 테스트에서 시간을 교체할 수 있게 하기 위해서입니다.

#### `Endpoints/AccountEndpoints.cs`

`POST /api/v1/accounts/guest`를 등록합니다.

- 익명 접근을 허용합니다.
- `CreateGuestAccountHandler`에 생성 절차를 위임합니다.
- 생성된 플레이어 ID와 토큰을 `201 Created`로 반환합니다.

#### `Endpoints/GameStateEndpoints.cs`

`GET /api/v1/game-state`를 등록합니다.

- 그룹 전체에 인증을 요구합니다.
- 요청에서 플레이어 ID를 받지 않습니다.
- JWT의 `sub`에서 플레이어 ID를 읽습니다.
- `GetGameStateHandler` 결과를 200 또는 404로 변환합니다.
- 토큰이 없거나 잘못되면 인증 미들웨어가 401을 반환합니다.

## 6. `src/IdleGuild.Application/`

한 번의 사용자 행동을 완성하는 유스케이스를 구현할 프로젝트입니다.

현재 Application 기능은 다음과 같습니다.

- 게스트 계정 생성
- 게임 상태 조회
- 방치 보상 계산과 저장
- 영웅 강화
- 스테이지 도전
- 골드 변경 원장 기록

Application은 Domain 규칙을 호출하고 저장소 인터페이스와 트랜잭션 경계를 조정합니다. HTTP나 EF Core 세부사항은 알지 않게 유지합니다.

### `IdleGuild.Application.csproj`

Application 프로젝트 설정입니다.

- `net10.0` 클래스 라이브러리입니다.
- `Domain` 프로젝트만 참조합니다.
- DI 등록을 위한 최소 추상 패키지만 참조합니다.

### `DependencyInjection.cs`

`CreateGuestAccountHandler`와 `GetGameStateHandler`를 요청 범위의 서비스로 등록합니다. API가 각 Handler의 생성자 의존성을 직접 조립하지 않게 합니다.

### `Abstractions/`

Application이 Infrastructure의 구체 기술을 직접 참조하지 않도록 경계 인터페이스를 보관합니다.

#### `Abstractions/Authentication/AccessToken.cs`

발급된 토큰 문자열과 만료 UTC 시각을 표현하는 Application 값입니다.

#### `Abstractions/Authentication/IAccessTokenIssuer.cs`

플레이어 ID로 액세스 토큰을 발급하는 인터페이스입니다. Application은 JWT 구현을 모르며 Infrastructure가 실제 구현을 제공합니다.

#### `Abstractions/Persistence/IPlayerGameStateRepository.cs`

플레이어 상태 추가와 ID 조회를 정의합니다. Application이 `GameDbContext`나 EF Core를 직접 사용하지 않게 합니다.

#### `Abstractions/Persistence/IGameUnitOfWork.cs`

한 유스케이스에서 추적한 DB 변경을 `SaveChangesAsync`로 확정하는 인터페이스입니다.

#### `Abstractions/Persistence/IGoldLedgerRepository.cs`

검증된 골드 변경 원장을 현재 작업 단위에 추가하는 인터페이스입니다. Application이 EF Core를 직접 참조하지 않고도 게임 상태와 기능 영수증, 원장을 같은 저장에 포함하게 합니다.

### `Accounts/CreateGuest/`

게스트 계정 생성 유스케이스를 기능 단위로 묶습니다.

#### `Accounts/CreateGuest/CreateGuestAccountHandler.cs`

새 플레이어 ID 생성, 초기 Domain 상태 생성, 토큰 발급, Repository 추가, 작업 단위 저장을 순서대로 수행합니다.

#### `Accounts/CreateGuest/CreateGuestAccountResult.cs`

Handler가 API에 반환할 플레이어 ID, 토큰, 만료 시각을 표현합니다.

### `GameStates/GetGameState/`

현재 게임 상태 조회 유스케이스를 묶습니다.

#### `GameStates/GetGameState/GetGameStateHandler.cs`

인증 계층에서 전달받은 플레이어 ID로 Repository를 조회하고 Domain 객체를 Application 결과로 변환합니다.

#### `GameStates/GetGameState/GetGameStateResult.cs`

API가 응답 계약으로 변환할 게임 상태 데이터를 표현합니다.

## 7. `src/IdleGuild.Domain/`

게임에서 반드시 지켜야 하는 상태와 규칙을 표현합니다.

Domain은 ASP.NET Core, EF Core, PostgreSQL 패키지를 참조하지 않습니다. 게임 규칙이 특정 저장 기술에 종속되지 않도록 하기 위한 구조입니다.

### `IdleGuild.Domain.csproj`

Domain 프로젝트 설정입니다.

- `net10.0` 클래스 라이브러리입니다.
- 다른 IdleGuild 프로젝트 참조가 없습니다.
- 외부 NuGet 패키지도 없습니다.

Domain의 순수성을 확인할 때 가장 먼저 볼 파일입니다.

### `DomainAssembly.cs`

Domain 어셈블리를 코드에서 안정적으로 찾기 위한 빈 표식 클래스입니다.

현재 `DomainDependencyTests`가 `typeof(DomainAssembly).Assembly`를 통해 Domain DLL의 참조 목록을 검사할 때 사용합니다. 이후 자동 매핑이나 도메인 이벤트 등록에도 활용할 수 있습니다.

### `GameStates/`

플레이어의 지속되는 게임 진행 상태를 표현하는 Domain 객체를 보관합니다.

#### `GameStates/PlayerGameState.cs`

한 플레이어의 핵심 상태를 나타냅니다.

- `PlayerId`: 플레이어 식별자
- `Gold`: 보유 골드
- `HeroLevel`: 주 영웅 레벨
- `HighestStage`: 최고 도달 스테이지
- `CreatedAtUtc`: 생성 시각
- `LastIdleRewardClaimedAtUtc`: 마지막 방치 보상 정산 시각
- `Version`: 동시 변경 감지 버전

`Create` 팩터리 메서드는 빈 `Guid`와 기본 시각을 거부하고, 시각을 UTC로 변환합니다. 새 플레이어는 골드 0, 영웅 레벨 1, 최고 스테이지 1로 생성됩니다.

속성의 setter를 `private`으로 둔 이유는 외부 코드가 검증 없이 게임 상태를 변경하지 못하게 하기 위해서입니다. 향후 골드 지급이나 강화는 의미 있는 Domain 메서드를 통해 수행합니다.

매개변수 없는 private 생성자는 EF Core가 DB 값을 객체로 복원할 때 필요합니다.

### `Economy/`

골드 변경을 운영 중 추적할 수 있는 재화 감사 Domain 객체를 보관합니다.

#### `Economy/GoldLedgerReason.cs`

방치 보상, 영웅 강화, 스테이지 체크포인트라는 골드 변경 사유를 안정적인 정수 값으로 구분합니다.

#### `Economy/GoldLedgerEntry.cs`

변경 전 잔액, 증감량, 변경 후 잔액과 멱등 참조 키를 보관합니다. `Create` 팩터리는 잔액 등식, 음수가 아닌 잔액, 0이 아닌 증감량, UTC 시각과 참조 키 길이를 검증합니다.

## 8. `src/IdleGuild.Infrastructure/`

Application과 Domain이 필요로 하는 기능을 PostgreSQL, EF Core 같은 실제 기술로 구현합니다.

### `IdleGuild.Infrastructure.csproj`

Infrastructure 프로젝트 설정입니다.

- `Application`과 `Domain`을 참조합니다.
- EF Core 10.0.9를 참조합니다.
- EF 설계 도구 패키지를 참조합니다.
- Npgsql PostgreSQL 공급자를 참조합니다.
- JWT 생성과 서명을 위한 IdentityModel 패키지를 참조합니다.

`Microsoft.EntityFrameworkCore.Design`에 `PrivateAssets=all`을 지정해 설계 전용 패키지가 다른 프로젝트와 최종 배포물로 전파되지 않게 합니다.

### `DependencyInjection.cs`

Infrastructure 서비스 등록 확장 메서드를 제공합니다.

`AddInfrastructure(connectionString, jwtOptions)`은 다음 작업을 수행합니다.

1. 연결 문자열이 비어 있지 않은지 검사합니다.
2. JWT 서명 키와 만료 설정을 검증합니다.
3. `GameDbContext`를 DI 컨테이너에 등록합니다.
4. EF Core Repository와 Unit of Work를 등록합니다.
5. JWT 액세스 토큰 발급 구현을 등록합니다.

Infrastructure 등록 세부사항을 `Program.cs`에서 분리하기 위해 만들었습니다.

### `Authentication/`

Application의 토큰 발급 추상화를 JWT 기술로 구현합니다.

#### `Authentication/JwtOptions.cs`

Issuer, Audience, SigningKey, 액세스 토큰 유효기간을 보관하고 시작 시 설정을 검증합니다. HMAC SHA-256을 위해 서명 키가 최소 32 UTF-8 바이트인지 확인합니다.

#### `Authentication/JwtAccessTokenIssuer.cs`

`IAccessTokenIssuer`를 구현합니다.

- 플레이어 ID를 `sub` Claim에 넣습니다.
- `jti`, `iat`, `exp`, `account_type` Claim을 생성합니다.
- HMAC SHA-256으로 JWT를 서명합니다.
- 토큰 문자열과 만료 시각을 Application에 반환합니다.

#### `Authentication/JwtTokenValidation.cs`

토큰 발급과 API 검증이 같은 Issuer, Audience, SigningKey, 알고리즘을 사용하도록 `TokenValidationParameters`를 생성합니다. 서명·발급자·대상·만료를 모두 필수로 검사합니다.

### `Persistence/`

데이터를 PostgreSQL에 영속화하는 EF Core 구현을 보관합니다.

#### `Persistence/GameDbContext.cs`

EF Core의 작업 단위이자 DB 세션입니다.

- `PlayerGameStates`를 통해 플레이어 상태를 조회·추가합니다.
- `GoldLedgerEntries`를 통해 골드 감사 원장을 조회·추가합니다.
- Application의 `IGameUnitOfWork`를 구현합니다.
- 같은 어셈블리의 모든 `IEntityTypeConfiguration`을 자동으로 적용합니다.
- 변경 추적 후 `SaveChangesAsync`를 호출하면 SQL을 생성해 DB에 반영합니다.

한 요청에서 조회와 변경을 같은 `GameDbContext`로 처리하면 하나의 작업 단위로 관리할 수 있습니다.

#### `Persistence/GameDbContextFactory.cs`

EF CLI 전용 `IDesignTimeDbContextFactory` 구현입니다.

Migration 생성 시 API 서버 전체를 실행하지 않고 `GameDbContext`를 만들 수 있게 합니다. 환경 변수에 연결 문자열이 있으면 사용하고, 없으면 설계 전용 자리표시자 연결 문자열을 사용합니다.

이 클래스는 실제 게임 요청 처리보다 `dotnet-ef`의 설계 시점 작업을 지원합니다.

### `Persistence/Configurations/`

Domain 객체와 DB 스키마의 매핑 규칙을 보관합니다.

Domain 클래스에 EF 특성(Attribute)을 붙이지 않고 Infrastructure에서 매핑해 Domain의 기술 독립성을 유지합니다.

#### `Persistence/Configurations/PlayerGameStateConfiguration.cs`

`PlayerGameState`를 `player_game_states` 테이블에 매핑합니다.

- `player_id`를 기본키로 지정합니다.
- C# 속성을 snake_case DB 열 이름으로 연결합니다.
- 골드가 0 이상인지 검사하는 DB 제약조건을 만듭니다.
- 영웅 레벨과 최고 스테이지가 1 이상인지 검사합니다.
- `Version`을 PostgreSQL의 `xmin` 시스템 열과 연결합니다.

Domain 검증과 DB 제약조건을 함께 사용하는 이유는 애플리케이션 버그나 직접 SQL 실행이 있어도 잘못된 데이터가 최종 저장되지 않게 방어하기 위해서입니다.

#### `Persistence/Configurations/GoldLedgerEntryConfiguration.cs`

`GoldLedgerEntry`를 `gold_ledger_entries`에 매핑합니다. 잔액 등식과 증감량을 Check Constraint로 다시 검증하고 `(player_id, reason, reference_id)` 유일 인덱스로 같은 기능 요청의 중복 원장을 막습니다.

### `Persistence/Repositories/`

Application 저장소 인터페이스의 EF Core 구현을 보관합니다.

#### `Persistence/Repositories/PlayerGameStateRepository.cs`

`IPlayerGameStateRepository`를 구현합니다.

- 새 `PlayerGameState`를 DbContext 변경 추적기에 추가합니다.
- 플레이어 ID로 상태를 읽기 전용 조회합니다.
- 조회에는 `AsNoTracking`을 사용해 불필요한 변경 추적을 줄입니다.

#### `Persistence/Repositories/GoldLedgerRepository.cs`

검증된 `GoldLedgerEntry`를 현재 `GameDbContext`의 변경 추적기에 추가합니다. Handler가 기능 영수증과 함께 `SaveChangesAsync`를 호출하므로 원장도 같은 DB 트랜잭션에 포함됩니다.

### `Persistence/Migrations/`

DB 스키마 변경 이력을 시간 순서대로 보관합니다.

Migration 파일은 빈 DB를 현재 구조로 만들고, 기존 DB를 다음 버전으로 안전하게 변경하기 위해 필요합니다.

#### `20260630131546_InitialGameState.cs`

첫 번째 Migration의 실행 코드입니다.

- `Up`: `player_game_states` 테이블과 제약조건을 생성합니다.
- `Down`: 해당 테이블을 제거해 Migration을 되돌립니다.

파일명 앞 숫자는 Migration이 만들어진 시각이며 실행 순서를 결정합니다. 이 파일은 EF가 생성하지만 실제 DB 변경 내용을 이해하기 위해 반드시 검토해야 합니다.

#### `20260630131546_InitialGameState.Designer.cs`

첫 Migration이 생성될 당시의 상세 EF 모델 정보입니다.

- Migration과 모델 메타데이터를 연결합니다.
- EF CLI가 자동 생성합니다.
- 특별한 이유가 없다면 직접 수정하지 않습니다.

#### `GameDbContextModelSnapshot.cs`

마지막 Migration까지 적용된 현재 EF 모델의 스냅샷입니다.

새 Migration을 만들 때 EF는 현재 C# 모델과 이 스냅샷을 비교해 변경점을 계산합니다. 수동 수정하면 다음 Migration이 잘못 생성될 수 있으므로 EF CLI가 관리하게 둡니다.

## 9. `tests/IdleGuild.Api.Tests/`

HTTP 계층이 실제 요청과 같은 방식으로 동작하는지 검증합니다.

### `IdleGuild.Api.Tests.csproj`

API 테스트 프로젝트 설정입니다.

- xUnit과 Test SDK를 참조합니다.
- 코드 커버리지 수집 패키지를 참조합니다.
- `Microsoft.AspNetCore.Mvc.Testing`을 참조합니다.
- 실제 `IdleGuild.Api`, `Application`, `Domain` 프로젝트를 참조합니다.

### `IdleGuildApiFactory.cs`

`WebApplicationFactory<Program>`으로 메모리 안에서 실제 API 서버 파이프라인을 시작합니다.

Development 환경으로 실행해 OpenAPI와 Swagger UI까지 검증할 수 있게 합니다. 실제 네트워크 포트를 열 필요가 없어 테스트가 빠르고 독립적입니다.

테스트에서는 PostgreSQL Repository와 Unit of Work를 메모리 저장소로 교체합니다. JWT 발급과 검증은 실제 구현을 그대로 사용해 인증 파이프라인을 검증합니다.

### `InMemoryPlayerGameStateStore.cs`

API 테스트에서 사용할 `IPlayerGameStateRepository`와 `IGameUnitOfWork` 메모리 구현입니다. HTTP 인증 테스트를 PostgreSQL 가용성과 분리하기 위해 만들었습니다.

### `AccountAuthenticationTests.cs`

게스트 인증 HTTP 시나리오를 검증합니다.

- 토큰 없는 게임 상태 요청이 401인지 확인합니다.
- 게스트 토큰으로 생성된 본인 상태를 조회합니다.
- 서로 다른 토큰이 서로 다른 플레이어 상태를 조회하는지 확인합니다.
- payload를 변조한 JWT가 401인지 확인합니다.

### `SystemEndpointTests.cs`

시스템 관련 HTTP 동작을 검증합니다.

- `/health`가 `Healthy`를 반환하는지 확인합니다.
- 상태 API가 `ok`와 서버 UTC 시각을 반환하는지 확인합니다.
- OpenAPI 문서에 상태 API 경로가 포함되는지 확인합니다.
- OpenAPI에 게스트·게임 상태 API와 Bearer 스키마가 포함되는지 확인합니다.
- Development 환경에서 Swagger UI가 열리는지 확인합니다.

Endpoint를 수정하면 해당 외부 동작을 이 테스트에서도 갱신합니다.

## 10. `tests/IdleGuild.Application.Tests/`

Application 유스케이스의 처리 순서와 결과를 DB·HTTP 없이 검증합니다.

### `IdleGuild.Application.Tests.csproj`

xUnit, Test SDK, 코드 커버리지 패키지와 `Application`, `Domain` 프로젝트를 참조합니다.

### `TestDoubles.cs`

테스트 전용 메모리 Repository·Unit of Work, 고정 토큰 발급기, 고정 `TimeProvider`를 제공합니다.

### `CreateGuestAccountHandlerTests.cs`

게스트 생성 Handler가 초기 상태를 한 번 저장하고, 같은 플레이어 ID로 토큰을 발급하며, 결과를 반환하는지 검증합니다.

### `GetGameStateHandlerTests.cs`

기존 플레이어 상태가 정확한 결과로 변환되는지와 존재하지 않는 플레이어가 `null`로 반환되는지 검증합니다.

## 11. `tests/IdleGuild.Domain.Tests/`

프레임워크나 DB 없이 순수 게임 규칙을 검증합니다. 가장 빠르게 실행되어야 하는 테스트 계층입니다.

### `IdleGuild.Domain.Tests.csproj`

Domain 테스트 프로젝트 설정입니다.

- xUnit, Test SDK, 코드 커버리지 패키지를 참조합니다.
- `IdleGuild.Domain`만 프로젝트 참조합니다.

### `DomainDependencyTests.cs`

Domain 어셈블리가 다른 `IdleGuild.*` 프로젝트를 참조하지 않는지 검사합니다.

실수로 Domain에서 EF Core나 API 계층 코드를 사용해 의존성 방향이 깨지는 것을 자동으로 막습니다.

### `PlayerGameStateTests.cs`

`PlayerGameState` 생성 규칙을 검증합니다.

- 신규 플레이어가 골드 0, 레벨 1, 스테이지 1인지 확인합니다.
- 생성 시각이 UTC로 변환되는지 확인합니다.
- 빈 플레이어 ID가 거부되는지 확인합니다.

게임 상태 규칙이 추가되면 DB 테스트보다 먼저 이곳에 빠른 단위 테스트를 추가합니다.

## 12. `tests/IdleGuild.Infrastructure.Tests/`

EF Core 매핑과 Migration이 실제 PostgreSQL에서 동작하는지 검증합니다.

### `IdleGuild.Infrastructure.Tests.csproj`

Infrastructure 테스트 프로젝트 설정입니다.

- xUnit과 Test SDK를 참조합니다.
- Testcontainers PostgreSQL 패키지를 참조합니다.
- `Infrastructure`와 `Domain` 프로젝트를 참조합니다.

### `PostgreSqlDatabaseFixture.cs`

Infrastructure 테스트에서 공유할 PostgreSQL 환경을 준비합니다.

기본 동작은 다음과 같습니다.

1. PostgreSQL 18 일회용 컨테이너를 생성합니다.
2. 테스트 DB와 사용자를 설정합니다.
3. 컨테이너를 시작합니다.
4. 모든 EF Migration을 적용합니다.
5. 테스트 종료 후 컨테이너와 임시 데이터를 제거합니다.

Docker를 직접 사용할 수 없는 CI 환경에서는 `IDLEGUILD_TEST_POSTGRES_CONNECTION_STRING`으로 외부 테스트 DB를 지정할 수도 있습니다.

동시성 테스트는 이전 테스트가 사용한 물리 연결 상태의 영향을 받지 않도록 연결 풀을 끄고 각 DbContext가 독립 연결을 사용합니다.

### `PlayerGameStatePersistenceTests.cs`

`PlayerGameState`의 실제 PostgreSQL 저장·조회 왕복을 검증합니다.

1. Domain 팩터리로 상태를 생성합니다.
2. 첫 번째 EF Repository로 저장합니다.
3. 두 번째 EF Repository로 다시 조회합니다.
4. 골드, 레벨, 스테이지, UTC 시각을 비교합니다.
5. PostgreSQL이 `xmin` 버전을 생성했는지 확인합니다.

서로 다른 DbContext를 사용하는 이유는 첫 Context의 메모리 캐시가 아니라 DB에서 실제로 읽은 값인지 보장하기 위해서입니다.

## 13. 현재 실행 흐름

### 시스템 상태 API

```text
클라이언트
    |
    | GET /api/v1/system/status
    v
Program.cs
    |
    | MapSystemEndpoints()
    v
SystemEndpoints.cs
    |
    | TimeProvider.GetUtcNow()
    v
SystemStatusResponse
    |
    | JSON 직렬화
    v
클라이언트 응답
```

### 게임 상태 저장

```text
PlayerGameState.Create()
    |
    | Domain 생성 규칙
    v
PlayerGameStateRepository.Add()
    |
    v
GameDbContext
    |
    | PlayerGameStateConfiguration
    v
Npgsql이 SQL 생성
    |
    v
PostgreSQL player_game_states
```

### 게스트 생성과 인증 상태 조회

```text
POST /api/v1/accounts/guest
    |
    v
CreateGuestAccountHandler
    ├─> PlayerGameState.Create()
    ├─> JwtAccessTokenIssuer.Issue()
    └─> Repository + UnitOfWork
              |
              v
         PostgreSQL 저장
    |
    v
playerId + accessToken 반환
    |
    | Authorization: Bearer <token>
    v
JWT Bearer 서명·issuer·audience·만료·sub 검증
    |
    v
GetGameStateHandler
    |
    v
본인 PlayerGameState만 조회
```

## 14. 앞으로 파일을 어디에 추가할지 판단하는 방법

| 만들려는 것 | 위치 |
| --- | --- |
| 게임 상태와 계산 규칙 | `src/IdleGuild.Domain/` |
| 한 번의 사용자 행동과 처리 순서 | `src/IdleGuild.Application/` |
| DB, 토큰, 외부 서비스 실제 구현 | `src/IdleGuild.Infrastructure/` |
| URL, HTTP 요청·응답 DTO | `src/IdleGuild.Api/` |
| 게임 규칙 단위 테스트 | `tests/IdleGuild.Domain.Tests/` |
| Application 유스케이스 테스트 | `tests/IdleGuild.Application.Tests/` |
| HTTP 통합 테스트 | `tests/IdleGuild.Api.Tests/` |
| PostgreSQL 통합 테스트 | `tests/IdleGuild.Infrastructure.Tests/` |
| 게임·구조·운영 설명 | `Docs/` |

예를 들어 게스트 계정 생성을 구현한다면 다음과 같이 나눕니다.

```text
Domain
  계정과 게임 상태의 유효한 생성 규칙

Application
  게스트 생성 유스케이스와 저장소 인터페이스

Infrastructure
  EF Core 저장소와 인증 토큰 구현

Api
  POST /api/v1/accounts/guest와 요청·응답 계약

Tests
  각 계층의 규칙, 저장, HTTP 결과 검증
```

## 15. 추천 코드 분석 순서

1. `README.md`에서 목적과 실행 방법을 확인합니다.
2. `Docs/GAME_DESIGN.md`에서 만들어야 할 게임을 이해합니다.
3. `Docs/ARCHITECTURE.md`에서 계층 책임을 확인합니다.
4. 각 `.csproj`의 `ProjectReference`로 의존성 방향을 확인합니다.
5. `Program.cs`에서 서버 시작과 DI 구성을 따라갑니다.
6. `Endpoints`에서 URL의 진입점을 찾습니다.
7. `Domain`에서 실제 게임 상태와 규칙을 읽습니다.
8. `Application`에서 유스케이스 처리 순서를 읽습니다.
9. `Infrastructure`에서 DB 저장 방식을 확인합니다.
10. 같은 이름의 테스트에서 기대 동작과 예외 조건을 확인합니다.

새 기능을 분석할 때는 다음 질문을 순서대로 사용하면 됩니다.

```text
어떤 URL로 요청이 들어오는가?
→ 어떤 Application 유스케이스가 실행되는가?
→ 어떤 Domain 규칙을 사용하는가?
→ 어떤 Infrastructure 구현으로 저장하는가?
→ 어떤 테스트가 이 동작을 보장하는가?
```

현재는 Step 8-2까지 완료되어 게스트 생성, 방치 보상, 영웅 강화, 스테이지 진행과 생산 보너스가 핵심 플레이 루프를 연결하고 API 오류 응답, 전역 예외 처리, 요청 로깅을 정리했습니다. 다음 Step은 배포와 포트폴리오 마감을 진행합니다.

## 16. Step 5에서 추가된 파일

- `Docs/IDLE_REWARDS.md`: 계산식, API 사용법, 중복·동시 요청 방어 이유를 설명합니다.
- `Domain/Rewards/IdleRewardPolicy.cs`: 초당 생산량과 8시간 상한을 한곳에 둡니다.
- `Domain/Rewards/IdleRewardSettlement.cs`: 한 번의 계산 결과를 값 객체로 전달합니다.
- `Domain/Rewards/IdleRewardClaimReceipt.cs`: 최초 지급 결과를 재생할 영수증 모델입니다.
- `Application/Rewards/ClaimIdleReward/*`: 방치 보상 유스케이스와 반환 모델입니다.
- `Application/Abstractions/Persistence/IIdleRewardClaimRepository.cs`: Application이 기술과 무관하게 영수증을 조회·추가하게 합니다.
- `Application/Abstractions/Persistence/PersistenceConflictException.cs`: DB별 충돌 예외를 Application 언어로 바꿉니다.
- `Api/Contracts/IdleRewardClaimResponse.cs`: 보상 API의 JSON 응답 계약입니다.
- `Api/Endpoints/RewardsEndpoints.cs`: 인증과 멱등 키를 검사하고 Handler 결과를 HTTP로 바꿉니다.
- `Infrastructure/Persistence/EfGameUnitOfWork.cs`: 저장 충돌을 변환하고 재시도 전 추적 상태를 비웁니다.
- `Infrastructure/Persistence/Configurations/IdleRewardClaimReceiptConfiguration.cs`: 영수증 테이블과 복합키·제약조건을 정의합니다.
- `Infrastructure/Persistence/Repositories/IdleRewardClaimRepository.cs`: 영수증 조회와 추가를 EF Core로 구현합니다.
- `Infrastructure/Persistence/Migrations/*AddIdleRewardClaims*`: 새 영수증 테이블을 재현 가능한 스키마 이력으로 만듭니다.
- `tests/*/IdleReward*Tests.cs`: 계산 규칙, Handler 멱등성, HTTP 계약, 실제 PostgreSQL 동시성을 계층별로 검증합니다.
- `tests/IdleGuild.Infrastructure.Tests/PostgreSqlTestCollection.cs`: DB 통합 테스트가 하나의 Fixture를 안전하게 공유하게 합니다.

## 17. Step 6에서 추가된 파일

- `Docs/HERO_UPGRADES.md`: 비용 공식, API 계약, 실패 멱등성, 동시성 방어를 설명합니다.
- `Domain/Requests/IdempotencyPolicy.cs`: 모든 상태 변경 API의 멱등 키 길이 규칙을 공유합니다.
- `Domain/Heroes/HeroUpgradePolicy.cs`: 강화 비용을 정확한 정수 연산으로 계산하고 최대 레벨을 정의합니다.
- `Domain/Heroes/HeroUpgradeOutcome.cs`: 성공, 골드 부족, 최대 레벨 판정을 구분합니다.
- `Domain/Heroes/HeroUpgradeSettlement.cs`: 한 번의 강화 판정 직후 결과를 전달합니다.
- `Domain/Heroes/HeroUpgradeReceipt.cs`: 성공과 실패의 최초 판정을 재생할 영수증 모델입니다.
- `Application/Heroes/UpgradeMainHero/*`: 강화 처리 순서, 충돌 재시도, 반환 모델을 구현합니다.
- `Application/Abstractions/Persistence/IHeroUpgradeReceiptRepository.cs`: 강화 영수증 저장 기술을 Application에서 분리합니다.
- `Api/Contracts/HeroUpgradeResponse.cs`: Unity가 받을 강화 JSON 응답 계약입니다.
- `Api/Endpoints/HeroesEndpoints.cs`: JWT·멱등 키를 검사하고 판정을 HTTP 상태로 변환합니다.
- `Infrastructure/Persistence/Configurations/HeroUpgradeReceiptConfiguration.cs`: 강화 영수증 테이블과 무결성 제약을 정의합니다.
- `Infrastructure/Persistence/Repositories/HeroUpgradeReceiptRepository.cs`: 강화 영수증 조회·추가를 EF Core로 구현합니다.
- `Infrastructure/Persistence/Migrations/*AddHeroUpgrades*`: 강화 영수증 테이블의 스키마 이력입니다.
- `tests/*/HeroUpgrade*Tests.cs`: 비용, 유스케이스, HTTP, 실제 DB 동시성을 계층별로 검증합니다.

## 18. Step 7에서 추가된 파일

- `Docs/STAGE_PROGRESSION.md`: 전투 공식, 진행 순서, 생산 보너스와 소급 방지를 설명합니다.
- `Domain/Stages/StageChallengePolicy.cs`: 전투력, 요구 전투력, 최대 스테이지와 보너스를 계산합니다.
- `Domain/Stages/StageChallengeOutcome.cs`: 성공과 세 가지 진행 실패를 구분합니다.
- `Domain/Stages/StageChallengeSettlement.cs`: 한 번의 도전 판정 직후 상태를 전달합니다.
- `Domain/Stages/StageChallengeReceipt.cs`: 최초 도전 결과를 재생할 영수증 모델입니다.
- `Domain/Rewards/IdleGoldCalculation.cs`: 정수 골드, 1/100 잔여값, 생산 배율 계산 결과입니다.
- `Application/Stages/ChallengeStage/*`: 멱등 도전, 경로 값 충돌, 저장 재시도를 구현합니다.
- `Application/Abstractions/Persistence/IStageChallengeReceiptRepository.cs`: 도전 영수증 저장 기술을 분리합니다.
- `Application/Abstractions/Persistence/IdempotencyKeyConflictException.cs`: 같은 키의 다른 명령 재사용을 표현합니다.
- `Api/Contracts/StageChallengeResponse.cs`: Unity가 받을 도전 JSON 계약입니다.
- `Api/Endpoints/StagesEndpoints.cs`: 스테이지 범위·JWT·키를 검사하고 HTTP 결과로 변환합니다.
- `Api/Endpoints/EndpointProblemResults.cs`: API 오류 응답과 멱등 키 검증을 공통화합니다.
- `Api/ErrorHandling/GlobalExceptionHandler.cs`: 예상하지 못한 예외를 500 `ProblemDetails`와 서버 로그로 변환합니다.
- `Infrastructure/Persistence/Configurations/StageChallengeReceiptConfiguration.cs`: 도전 테이블과 결과별 DB 제약을 정의합니다.
- `Infrastructure/Persistence/Repositories/StageChallengeReceiptRepository.cs`: 도전 영수증을 EF Core로 조회·추가합니다.
- `Infrastructure/Persistence/Migrations/*AddStageProgression*`: 영수증, 생산 잔여값, 보상 배율 열의 스키마 이력입니다.
- `tests/*/Stage*Tests.cs`: 규칙, 유스케이스, HTTP, 실제 DB 동시 진행을 계층별로 검증합니다.

## 19. Step 8-1에서 추가된 파일

- `Docs/API_ERRORS.md`: 클라이언트가 실패 응답을 처리할 수 있도록 `ProblemDetails`와 상태 코드 의미를 정리합니다.
- `Api/Endpoints/EndpointProblemResults.cs`: 반복되는 멱등 키 검증과 400·404·409·503 오류 응답 생성을 한곳에 모읍니다.
- `tests/IdleGuild.Api.Tests/*EndpointTests.cs`: 실패 응답이 상태 코드뿐 아니라 `ProblemDetails` 제목까지 만족하는지 검증합니다.

## 20. Step 8-2에서 추가된 파일

- `Docs/OBSERVABILITY.md`: 요청 로깅, 전역 예외 처리, `traceId` 활용 방식을 설명합니다.
- `Api/ErrorHandling/GlobalExceptionHandler.cs`: 처리되지 않은 예외를 로그와 500 `ProblemDetails`로 변환합니다.
- `tests/IdleGuild.Api.Tests/ErrorHandlingTests.cs`: 실제 HTTP 파이프라인에서 예외가 안전한 오류 계약으로 변환되는지 검증합니다.

## 21. Step 8-3 문서 보강에서 추가된 파일

- `Docs/STEP_DECISIONS.md`: Step 1부터 Step 8-2까지 왜 진행했는지, 어떤 문제를 해결했는지, 어떤 선택을 했는지, 어떻게 검증했는지 설명합니다.

## 22. Step 8-3 데모·배포 문서에서 추가된 파일

- `Docs/DEMO_SCENARIO.md`: 서버 실행 후 게스트 생성, 보상 수령, 영웅 강화, 스테이지 도전, 생산 보너스 확인까지 따라 하는 데모 가이드입니다.
- `Docs/DEPLOYMENT.md`: 운영 환경 변수, Migration, Health Check, 로그 기준, 배포 전 체크리스트를 정리합니다.
- `src/IdleGuild.Api/IdleGuild.Api.http`: 멱등 재시도와 오류 계약 확인 요청을 추가해 IDE에서 핵심 API를 순서대로 시험할 수 있게 했습니다.

## 23. Step 8-4 서버 MVP 마감에서 추가된 파일

- `Docs/SERVER_MVP_COMPLETE.md`: 서버 MVP 완료 기준, 포함 기능, 제외 범위, 테스트 검증 기준, Unity 클라이언트 전환 기준을 정리합니다.

## 24. Hardening Step 1 재화 변경 이력 원장에서 추가된 파일

- `Docs/GOLD_LEDGER.md`: 원장 목적, 기록 대상, 불변식, 트랜잭션 경계와 테스트 기준을 설명합니다.
- `Domain/Economy/GoldLedgerReason.cs`: 골드 변경 사유를 구분합니다.
- `Domain/Economy/GoldLedgerEntry.cs`: 변경 전 잔액, 증감량, 변경 후 잔액의 감사 불변식을 검증합니다.
- `Application/Abstractions/Persistence/IGoldLedgerRepository.cs`: 원장 추가를 저장 기술과 분리합니다.
- `Infrastructure/Persistence/Configurations/GoldLedgerEntryConfiguration.cs`: 원장 테이블과 DB 제약·인덱스를 정의합니다.
- `Infrastructure/Persistence/Repositories/GoldLedgerRepository.cs`: 원장을 EF Core 작업 단위에 추가합니다.
- `Infrastructure/Persistence/Migrations/*AddGoldLedger*`: `gold_ledger_entries` 스키마 변경 이력입니다.
- `tests/IdleGuild.Domain.Tests/GoldLedgerEntryTests.cs`: 원장 생성 불변식을 빠르게 검증합니다.
- 기존 Application·Infrastructure 테스트: 세 골드 변경 경로와 동시 요청의 원장 개수·금액을 검증하도록 확장했습니다.

## 25. Hardening Step 2 Rate Limiting에서 추가된 파일

- `Docs/RATE_LIMITING.md`: 적용 API, 분리 키, 제한량, 429 계약과 다중 인스턴스 한계를 설명합니다.
- `Api/RateLimiting/ApiRateLimitPolicies.cs`: IP·JWT 플레이어별 고정 윈도우와 공통 거부 응답을 구현합니다.
- `Api/Endpoints/AccountEndpoints.cs`: 익명 게스트 생성 정책을 연결합니다.
- `Api/Endpoints/RewardsEndpoints.cs`, `HeroesEndpoints.cs`, `StagesEndpoints.cs`: 인증 플레이어 상태 변경 정책을 연결합니다.
- `tests/IdleGuild.Api.Tests/RateLimitingTests.cs`: IP 제한 초과, 429 계약과 플레이어별 한도 격리를 검증합니다.

## 26. Hardening Step 3 관리자용 조회 API에서 추가된 파일

- `Docs/ADMIN_API.md`: 관리자 인증 경계, 상태·원장 API와 커서 사용법을 설명합니다.
- `Api/Authorization/AdminAuthorization.cs`: `account_type=admin` 권한 정책 상수를 정의합니다.
- `Api/Endpoints/AdminEndpoints.cs`: 읽기 전용 플레이어 상태와 원장 Endpoint를 등록합니다.
- `Api/Endpoints/AdminLedgerCursorCodec.cs`: 내부 키셋 위치와 URL 안전 커서를 상호 변환합니다.
- `Api/Contracts/Admin*Response.cs`: 관리자 HTTP 응답 계약을 정의합니다.
- `Application/Admin/Players/GetAdminPlayer/*`: 플레이어의 운영 상태 조회를 구현합니다.
- `Application/Admin/Players/GetGoldLedgerPage/*`: 최대 100개의 최신순 원장 페이지와 다음 위치를 계산합니다.
- `Application/Abstractions/Persistence/IGoldLedgerReader.cs`: 원장 조회를 PostgreSQL 기술과 분리합니다.
- `Infrastructure/Persistence/Repositories/GoldLedgerReader.cs`: 플레이어별 키셋 원장 쿼리를 EF Core로 구현합니다.
- `Infrastructure/Persistence/Migrations/*OptimizeGoldLedgerAdminQuery*`: 관리자 조회 정렬에 맞는 3열 인덱스를 추가합니다.
- `tests/IdleGuild.Application.Tests/AdminPlayerQueryHandlerTests.cs`: 상태 변환과 커서 페이지 연결을 검증합니다.
- `tests/IdleGuild.Api.Tests/AdminEndpointTests.cs`: 401·403·관리자 성공·커서 오류 계약을 검증합니다.
- `tests/IdleGuild.Infrastructure.Tests/GoldLedgerReaderTests.cs`: 실제 PostgreSQL 정렬과 키셋 조건을 검증합니다.

## 27. Hardening Step 4 컨테이너 배포 기초에서 추가된 파일

- `Dockerfile`: SDK 빌드와 ASP.NET Runtime 실행을 분리한 API 이미지 정의입니다.
- `.dockerignore`: 비밀값과 API 게시에 필요 없는 파일을 Build Context에서 제외합니다.
- `compose.api.yaml`: PostgreSQL과 API를 함께 실행할 선택적 Compose 구성입니다.
- `.env.example`: 컨테이너 API 포트, JWT Issuer·Audience·유효기간 예시를 추가했습니다.
- `Docs/CONTAINER_DEPLOYMENT.md`: 이미지 빌드부터 Migration, 실행, 검증과 정지까지 설명합니다.
- `Docs/DEPLOYMENT.md`: 컨테이너 배포와 Migration 분리 체크리스트를 반영했습니다.

## 28. Hardening Step 5 DB Readiness에서 추가된 파일

- `Docs/HEALTH_CHECKS.md`: `/health`와 `/ready`의 역할과 배포 플랫폼 사용 기준을 설명합니다.
- `Api/HealthChecks/PostgreSqlReadinessHealthCheck.cs`: DB probe 결과를 ASP.NET Core Health 상태로 변환합니다.
- `Infrastructure/HealthChecks/IDatabaseReadinessProbe.cs`: API Health Check와 DB 기술 사이의 테스트 가능한 경계입니다.
- `Infrastructure/HealthChecks/PostgreSqlReadinessProbe.cs`: EF Core `CanConnectAsync`로 실제 PostgreSQL 연결을 검사합니다.
- `tests/IdleGuild.Api.Tests/StubDatabaseReadinessProbe.cs`: API 테스트에서 DB 성공·실패를 결정론적으로 만듭니다.
- `tests/IdleGuild.Infrastructure.Tests/PostgreSqlReadinessProbeTests.cs`: 실제 Testcontainers PostgreSQL 연결을 검증합니다.
- `Dockerfile`: `/ready`를 호출하는 Docker `HEALTHCHECK`를 추가했습니다.
