# 게스트 인증

## 1. 목적

MVP 클라이언트가 별도 회원가입 없이 플레이를 시작하고, 이후 요청에서 자신의 게임 상태만 접근하게 하는 것이 목적입니다.

```text
POST /api/v1/accounts/guest
    |
    | 초기 PlayerGameState 저장
    v
playerId + JWT 반환
    |
    | Authorization: Bearer <token>
    v
GET /api/v1/game-state
```

클라이언트가 조회할 플레이어 ID를 직접 보내지 않고, 검증된 JWT의 `sub` Claim을 서버가 읽습니다. 따라서 다른 플레이어 ID를 URL이나 Body에 넣어 조회하는 경로 자체가 없습니다.

## 2. 게스트 생성

`POST /api/v1/accounts/guest`는 인증 없이 호출할 수 있습니다.

서버는 다음 순서로 처리합니다.

1. 충돌 가능성이 매우 낮은 새 `Guid` 플레이어 ID를 생성합니다.
2. 서버 UTC 시각으로 초기 `PlayerGameState`를 생성합니다.
3. 같은 플레이어 ID를 `sub`로 갖는 액세스 토큰을 발급합니다.
4. 게임 상태를 PostgreSQL에 저장합니다.
5. 저장이 성공한 뒤 플레이어 ID, 토큰, 만료 시각을 반환합니다.

응답 예시는 다음과 같습니다.

```json
{
  "playerId": "470fa8f6-0b82-46f8-b188-1a5600890c7d",
  "accessToken": "<JWT>",
  "expiresAtUtc": "2026-08-03T00:00:00+00:00"
}
```

## 3. JWT Claim

| Claim | 역할 |
| --- | --- |
| `iss` | 토큰 발급 서버 |
| `aud` | 토큰을 사용할 클라이언트/API 대상 |
| `sub` | 플레이어 ID |
| `jti` | 토큰 고유 ID |
| `iat` | 발급 시각 |
| `exp` | 만료 시각 |
| `account_type` | 현재 계정 형식인 `guest` |

API는 서명, 발급자, 대상, 만료와 `sub`의 Guid 형식을 모두 검증합니다. 하나라도 잘못되면 보호된 API는 `401 Unauthorized`를 반환합니다.

## 4. 인증된 상태 조회

`GET /api/v1/game-state`는 Bearer JWT가 필요합니다.

```http
GET /api/v1/game-state
Authorization: Bearer <access-token>
```

처리 순서는 다음과 같습니다.

1. JWT Bearer 미들웨어가 서명, 발급자, 대상, 만료를 검증합니다.
2. `sub` Claim을 `Guid` 플레이어 ID로 변환합니다.
3. `GetGameStateHandler`가 해당 ID의 상태만 조회합니다.
4. 상태가 존재하면 `200 OK`, 없으면 `404 Not Found`를 반환합니다.

토큰이 없거나 변조됐다면 Handler까지 도달하지 않고 `401 Unauthorized`가 반환됩니다.

## 5. 설정과 비밀값

기본 설정 형식은 `appsettings.json`에 있지만 서명 키는 자리표시자입니다.

로컬 실행 시 다음처럼 환경 변수로 덮어씁니다.

```powershell
$env:Jwt__SigningKey = "replace_with_a_random_secret_of_at_least_32_bytes"
```

운영 서명 키는 Git에 커밋하지 않습니다. 현재 HMAC SHA-256 자체 발급 방식은 학습용 폐쇄형 MVP를 위한 선택입니다. 실제 공개 서비스에서는 비대칭 키와 표준 OIDC/OAuth 제공자를 사용하고, Application의 `IAccessTokenIssuer` 구현만 교체하는 방향을 권장합니다.

## 6. 현재 제한사항

- 게스트 토큰의 기본 유효기간은 30일입니다.
- Refresh Token은 아직 없습니다.
- 토큰을 잃어버린 게스트 계정의 복구 기능이 없습니다.
- 로그아웃 토큰 폐기 목록은 없습니다.
- 소셜 계정 연동은 MVP 이후 기능입니다.

이 제한은 포트폴리오에서 숨기지 않고 명시하며, 이후 인증 확장 Step에서 보완할 수 있습니다.

## 7. 테스트 범위

- 토큰 없는 게임 상태 요청은 401
- 정상 게스트 토큰은 본인의 초기 상태 조회
- 서로 다른 토큰은 서로 다른 플레이어 상태 조회
- payload를 변조한 토큰은 401
- Application Handler가 상태 저장과 토큰 발급을 한 번씩 수행
- 실제 PostgreSQL Repository가 플레이어 ID로 상태 저장·조회
