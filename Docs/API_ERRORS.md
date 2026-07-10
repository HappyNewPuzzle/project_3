# API 오류 계약

이 문서는 Unity 클라이언트가 서버 오류를 일관되게 처리할 수 있도록 HTTP 실패 응답의 공통 규칙을 정리합니다.

## 1. 기본 원칙

- 성공 응답은 각 기능별 응답 DTO를 반환합니다.
- 입력값, 멱등 키, 서버 재시도 초과 같은 일반 오류는 `ProblemDetails` 형식으로 반환합니다.
- 게임 규칙상 정상적으로 판정된 실패는 오류가 아니라 기능 응답 DTO로 반환합니다.
- 클라이언트는 같은 요청을 재시도할 때 같은 `Idempotency-Key`를 유지해야 합니다.

## 2. ProblemDetails 형식

오류 응답 Body는 ASP.NET Core의 표준 `ProblemDetails` JSON을 사용합니다.

```json
{
  "type": "about:blank",
  "title": "Idempotency key is required.",
  "status": 400,
  "detail": "Idempotency-Key header is required."
}
```

클라이언트는 우선 `status`로 분기하고, 화면 표시나 로그에는 `title`과 `detail`을 사용할 수 있습니다.

## 3. 공통 상태 코드

| HTTP 상태 | 의미 | 클라이언트 처리 방향 |
| --- | --- | --- |
| `400 Bad Request` | 요청 경로, 헤더, 입력값이 잘못됨 | 요청 생성 코드를 수정하거나 사용자 입력을 다시 받습니다. |
| `401 Unauthorized` | 토큰이 없거나 유효하지 않음 | 게스트 계정 생성 또는 재로그인을 진행합니다. |
| `403 Forbidden` | 인증은 됐지만 관리자 같은 필수 권한이 없음 | 해당 기능에 맞는 계정 권한을 확인합니다. |
| `404 Not Found` | 인증된 플레이어의 게임 상태를 찾을 수 없음 | 계정 생성 흐름을 다시 시작합니다. |
| `409 Conflict` | 멱등 키를 다른 명령에 재사용했거나 게임 규칙상 실패 판정 | Body 형식에 따라 처리합니다. |
| `429 Too Many Requests` | IP 또는 플레이어의 허용 호출량을 초과함 | `Retry-After`만큼 기다린 뒤 같은 멱등 키로 재시도합니다. |
| `503 Service Unavailable` | 내부 재시도 후에도 저장 충돌이 지속됨 | 같은 `Idempotency-Key`로 잠시 뒤 재시도합니다. |
| `500 Internal Server Error` | 예상하지 못한 서버 예외 | `traceId`를 로그에 남기고 잠시 뒤 재시도합니다. |

## 4. 멱등 키 오류

보상 수령, 영웅 강화, 스테이지 도전은 모두 `Idempotency-Key` 헤더가 필요합니다.

```http
Idempotency-Key: claim-20260707-001
```

키가 없거나 공백이면 다음 오류가 반환됩니다.

```json
{
  "title": "Idempotency key is required.",
  "status": 400,
  "detail": "Idempotency-Key header is required."
}
```

키가 너무 길면 다음 오류가 반환됩니다.

```json
{
  "title": "Idempotency key is too long.",
  "status": 400,
  "detail": "Idempotency-Key cannot exceed 64 characters."
}
```

## 5. 요청 속도 제한 오류

게스트 생성 또는 인증된 상태 변경 요청이 허용량을 넘으면 429 `ProblemDetails`가 반환됩니다. 응답의 `Retry-After` 헤더와 `retryAfterSeconds` 확장값은 다시 시도하기 전 기다릴 초를 나타냅니다.

상태 변경 요청은 기다린 뒤에도 최초 요청과 같은 `Idempotency-Key`를 사용해야 합니다. 자세한 정책은 [API 요청 속도 제한](RATE_LIMITING.md)에 정리되어 있습니다.

## 6. 게임 규칙 실패와 HTTP 오류의 차이

서버가 요청을 이해했고 게임 규칙 판정까지 완료했다면 기능 응답 DTO를 반환합니다.

예를 들어 영웅 강화에서 골드가 부족하면 HTTP `409 Conflict`이지만 Body는 `HeroUpgradeResponse`입니다.

```json
{
  "outcome": "insufficientGold",
  "goldCost": 10,
  "isReplay": false
}
```

반대로 헤더가 빠졌거나 같은 멱등 키를 다른 스테이지에 재사용한 경우는 요청 계약 위반이므로 `ProblemDetails`를 반환합니다.

## 7. 현재 구현 위치

- `src/IdleGuild.Api/Endpoints/EndpointProblemResults.cs`: 공통 오류 응답과 멱등 키 검증
- `src/IdleGuild.Api/ErrorHandling/GlobalExceptionHandler.cs`: 예상하지 못한 예외를 500 오류 계약으로 변환
- `src/IdleGuild.Api/RateLimiting/ApiRateLimitPolicies.cs`: IP·플레이어별 제한과 429 오류 계약
- `src/IdleGuild.Api/Endpoints/AdminEndpoints.cs`: 관리자 권한, 페이지 입력과 조회 오류 변환
- `src/IdleGuild.Api/Endpoints/RewardsEndpoints.cs`: 방치 보상 오류 변환
- `src/IdleGuild.Api/Endpoints/HeroesEndpoints.cs`: 영웅 강화 오류 변환
- `src/IdleGuild.Api/Endpoints/StagesEndpoints.cs`: 스테이지 도전 오류 변환
- `tests/IdleGuild.Api.Tests/*EndpointTests.cs`: HTTP 상태 코드와 `ProblemDetails` 검증
- `tests/IdleGuild.Api.Tests/RateLimitingTests.cs`: 제한 초과와 플레이어 분리 검증
