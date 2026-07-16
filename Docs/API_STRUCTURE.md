# IdleGuild.Api 폴더와 파일 구조

이 문서는 현재 `src/IdleGuild.Api`의 모든 소스 폴더와 파일이 왜 존재하며 어떤 책임을 갖는지 설명합니다. `bin`과 `obj`는 빌드 시 자동 생성되므로 제외합니다.

## 1. API 프로젝트의 책임

```text
Unity / 운영 도구 → HTTP·JSON·JWT → IdleGuild.Api
    → Application Handler → Domain 규칙 → Infrastructure 저장
```

API는 URL·HTTP Method, 요청 검증, 인증·인가, Handler 호출, JSON·상태 코드 변환, 오류 계약, Rate Limit, Health Check, OpenAPI와 관측성을 담당합니다. 재화 계산, 장비 능력치와 승패 같은 게임 규칙은 Domain과 Application에 둡니다.

## 2. 루트 파일

### `IdleGuild.Api.csproj`

ASP.NET Core Web 프로젝트의 .NET 대상 버전, OpenAPI·Swagger·JWT 패키지와 Application·Infrastructure 참조를 정의합니다.

### `Program.cs`

프로세스 진입점이자 Composition Root입니다. 로그, OpenAPI, Health Check, ProblemDetails, Rate Limit, Application·Infrastructure, JWT와 관리자 Policy를 등록합니다. 관측성·예외 처리·HTTP Logging·인증·Rate Limit·인가 Middleware를 연결하고 모든 기능 Endpoint를 Map합니다. `partial Program`은 통합 테스트가 실제 파이프라인을 실행하게 합니다.

### `appsettings.json`

PostgreSQL, JWT와 로그의 기본 설정입니다. 실제 비밀번호와 서명 키는 환경 변수나 User Secrets로 덮어씁니다.

### `appsettings.Development.json` (로컬 전용, Git 제외)

개발 PC에서 기본 설정을 덮어쓰는 선택 파일입니다. 비밀값이 들어갈 수 있어 `.gitignore` 대상이며 저장소의 공식 소스 파일로 취급하지 않습니다. 공유할 설정 예시는 루트 `.env.example`과 배포 문서에 기록합니다.

### `IdleGuild.Api.http`

IDE HTTP Client에서 핵심 API를 수동 호출하는 예제입니다. 자동 테스트를 대체하지 않고 개발 중 빠른 확인에 사용합니다.

## 3. `Authentication/`

### `ClaimsPrincipalExtensions.cs`

검증된 JWT `sub` Claim을 `Guid` 플레이어 ID로 변환합니다. 플레이어 ID를 Body로 신뢰하지 않아 다른 계정 접근을 막습니다.

## 4. `Authorization/`

### `AdminAuthorization.cs`

관리자 Policy 이름, `account_type` Claim과 `admin` 값을 정의해 Program과 Endpoint가 같은 권한 상수를 공유합니다.

## 5. `Contracts/`

Unity와 운영 도구가 주고받는 JSON DTO입니다. Domain 객체를 직접 노출하지 않아 내부 변경과 외부 계약을 분리합니다.

### 시스템·계정·상태

| 파일 | 역할 |
| --- | --- |
| `SystemStatusResponse.cs` | 서버 상태와 UTC 시각 |
| `GuestAccountResponse.cs` | 플레이어 ID, JWT와 만료 시각 |
| `GameStateResponse.cs` | 골드, 레벨, 스테이지, 생산 보너스, 총 전투력과 장비 보너스 |
| `UpdateSelectedHeroRequest.cs` / `UpdateSelectedHeroResponse.cs` | 안정적인 문자열 영웅 ID 변경 계약 |
| `IdleRewardPreviewResponse.cs` | 서버 시각 기준 방치 보상 미리보기 계약 |

### 게임 행동

| 파일 | 역할 |
| --- | --- |
| `IdleRewardClaimResponse.cs` | 방치 시간, 지급 골드, 잔액과 재생 여부 |
| `HeroUpgradeResponse.cs` | 강화 결과, 레벨, 비용, 잔액과 재생 여부 |
| `StageChallengeResponse.cs` | 승패, 전투력, 스테이지, 보상과 재생 여부 |

### 장비

| 파일 | 역할 |
| --- | --- |
| `EquipmentItemResponse.cs` | 장비 인스턴스, 슬롯, 능력치와 장착 여부 |
| `EquipmentInventoryResponse.cs` | 보유 목록과 장착 전투력 합계 |
| `ChangeEquipmentRequest.cs` | 목표 장착 상태 입력 |
| `ChangeEquipmentResponse.cs` | 장착 결과, 교체 장비와 재생 여부 |

### 모의 상점

| 파일 | 역할 |
| --- | --- |
| `ShopProductResponse.cs` | 상품 ID, 이름, 모의 가격과 지급 골드 |
| `ShopCatalogResponse.cs` | 판매 상품 목록 |
| `ShopPurchaseResponse.cs` | 구매 ID, 상품, 지급 골드, 잔액과 재생 여부 |
| `ShopPurchaseHistoryResponse.cs` | 인증 플레이어 구매 영수증 목록 |

### 관리자 조회

| 파일 | 역할 |
| --- | --- |
| `AdminPlayerResponse.cs` | 플레이어 현재 상태 |
| `AdminGoldLedgerEntryResponse.cs` | 원장 한 행의 사유·전후 잔액·참조 ID·시각 |
| `AdminGoldLedgerPageResponse.cs` | 원장 페이지와 다음 커서 |

## 6. `Endpoints/`

Minimal API Route를 기능별로 등록하고 인증 정보와 HTTP 입력을 확인한 뒤 Handler 결과를 Contract와 상태 코드로 변환합니다.

| 파일 | API와 역할 |
| --- | --- |
| `SystemEndpoints.cs` | `GET /api/v1/system/status`; 서버 UTC 상태 |
| `AccountEndpoints.cs` | `POST /api/v1/accounts/guest`; 게스트 생성 |
| `GameStateEndpoints.cs` | `GET /api/v1/game-state`; 인증 플레이어 상태 |
| `RewardsEndpoints.cs` | 읽기 전용 방치 보상 미리보기와 멱등 수령 |
| `ProfileEndpoints.cs` | 인증 플레이어의 선택 영웅 저장 |
| `HeroesEndpoints.cs` | 멱등 주 영웅 강화 |
| `StagesEndpoints.cs` | 서버 전투력 기반 스테이지 판정 |
| `EquipmentEndpoints.cs` | 장비 목록과 장착·해제 |
| `ShopEndpoints.cs` | 상품 목록, 모의 구매와 구매 이력 |
| `AdminEndpoints.cs` | 관리자 상태와 골드 원장 조회 |
| `EndpointProblemResults.cs` | ProblemDetails와 멱등 키 검증 공통화 |
| `AdminLedgerCursorCodec.cs` | 원장 키셋 위치와 URL 안전 커서 변환 |

## 7. `ErrorHandling/`

### `GlobalExceptionHandler.cs`

처리되지 않은 예외를 서버에 기록하고 내부 정보가 노출되지 않는 500 ProblemDetails로 변환합니다. 예상 가능한 400·404·409 등은 각 Endpoint가 처리합니다.

## 8. `HealthChecks/`

### `PostgreSqlReadinessHealthCheck.cs`

Infrastructure DB probe를 Health 결과로 변환합니다. `/ready`에서 DB 연결 불가 시 503을 반환하며 `/health` liveness와 분리됩니다.

## 9. `Observability/`

### `ApiTelemetry.cs`

`IdleGuild.Api` Meter의 완료 요청 Counter, 5xx Counter와 응답 시간 Histogram을 정의합니다.

### `ApiObservabilityMiddleware.cs`

모든 응답에 `X-Trace-Id`를 추가하고 Method, Route Template, 상태 코드, 시간과 플레이어를 구조화 로그로 남깁니다. 메트릭에는 실제 URL이나 플레이어 ID를 넣지 않아 시계열 폭증을 막습니다.

## 10. `OpenApi/`

### `BearerSecuritySchemeTransformer.cs`

OpenAPI에 HTTP Bearer JWT Scheme을 등록해 Swagger UI의 Authorize 입력을 만듭니다.

### `BearerSecurityRequirementTransformer.cs`

`RequireAuthorization` Endpoint에만 Bearer 요구사항을 붙여 공개 API와 보호 API를 구분합니다.

## 11. `Properties/`

### `launchSettings.json`

로컬 URL `http://localhost:5219`, Development 환경과 IDE 실행 프로필을 정의합니다. 배포 설정은 아닙니다.

## 12. `RateLimiting/`

### `ApiRateLimitPolicies.cs`

- 게스트 생성: 연결 IP별 5회/분
- 플레이어 상태 변경: JWT `sub`별 합산 30회/분
- 관리자 조회: 관리자 `sub`별 합산 120회/분

429 ProblemDetails와 `Retry-After`를 생성합니다. 현재 카운터는 단일 프로세스 메모리에 있고 다중 인스턴스 기준은 [Redis 도입 의사결정](REDIS_DECISION.md)에 있습니다.

## 13. 코드를 분석하는 순서

`AdminGoldLedgerEntryResponse.cs`를 예로 들면 다음 순서로 읽습니다.

1. `AdminEndpoints.cs`에서 DTO 생성 위치와 URL을 찾습니다.
2. Endpoint가 호출하는 Application Handler를 찾습니다.
3. Handler의 저장소 인터페이스를 찾습니다.
4. Infrastructure Repository에서 PostgreSQL Query를 확인합니다.
5. `AdminEndpointTests`와 `GoldLedgerReaderTests`에서 계약과 DB 동작을 확인합니다.

일반 기능도 `Endpoint → Contract → Application Handler → Domain 규칙 → Repository → 테스트` 순서로 따라가면 계층 책임을 이해하기 쉽습니다.
