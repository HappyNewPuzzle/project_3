# 관리자 조회 API

이 문서는 Hardening Step 3에서 추가한 관리자 권한 경계와 플레이어 상태·골드 원장 조회 방법을 설명합니다.

## 1. 목적

운영 중에는 유저 문의, 재화 이상, 중복 지급 의심 상황에서 서버 데이터를 확인할 수 있어야 합니다. 일반 게임 클라이언트가 이 데이터에 접근하면 안 되므로 읽기 전용 관리자 경로와 별도 권한 정책으로 분리합니다.

현재 관리자 API는 다음 질문에 답합니다.

- 특정 플레이어의 현재 골드, 레벨, 스테이지는 무엇인가?
- 계정 생성 시각과 마지막 보상 정산 시각은 언제인가?
- 골드가 어떤 기능에서 얼마나 증가하거나 감소했는가?
- 원장의 변경 전·후 잔액과 멱등 참조 키는 무엇인가?

## 2. 인증과 권한

관리자 API도 일반 API와 같은 JWT 서명, 발급자, 대상, 만료 검증을 먼저 통과해야 합니다. 추가로 다음 Claim이 필요합니다.

```json
{
  "sub": "관리자 식별 GUID",
  "account_type": "admin"
}
```

| 요청 | 결과 |
| --- | --- |
| 토큰 없음 또는 잘못된 서명 | `401 Unauthorized` |
| 정상 게스트 토큰 | `403 Forbidden` |
| 정상 서명과 `account_type=admin` | 관리자 Endpoint 실행 |

공개 관리자 로그인이나 토큰 발급 Endpoint는 만들지 않았습니다. 운영자 인증 시스템이 신뢰할 수 있는 위치에서 관리자 JWT를 발급한다는 경계만 서버에 구현했습니다. 테스트에서는 실제 서명 검증을 통과하는 관리자 JWT를 테스트 코드가 생성합니다.

현재 게스트와 관리자는 같은 JWT Issuer 설정을 사용합니다. 실제 운영에서는 관리자 전용 Issuer·Audience·Signing Key 또는 조직의 Identity Provider로 분리하고 짧은 만료, MFA, 토큰 폐기를 추가하는 것이 권장됩니다.

## 3. 플레이어 상태 조회

```http
GET /api/v1/admin/players/{playerId}
Authorization: Bearer {adminJwt}
```

응답에는 플레이어 ID, 현재 골드, 영웅 레벨, 최고 스테이지와 생산 보너스, 1/100 골드 잔여값, 생성·마지막 보상 시각, PostgreSQL `xmin` 버전이 포함됩니다.

플레이어가 없으면 `404 ProblemDetails`를 반환합니다. 이 API는 조회 전용이며 상태 수정 기능을 제공하지 않습니다.

## 4. 골드 원장 조회

```http
GET /api/v1/admin/players/{playerId}/gold-ledger?pageSize=50&cursor={nextCursor}
Authorization: Bearer {adminJwt}
```

- 기본 `pageSize`는 50, 최소 1, 최대 100입니다.
- 최신 원장부터 반환합니다.
- 다음 데이터가 있으면 `nextCursor`를 반환합니다.
- 다음 요청은 반환된 커서를 수정하지 않고 그대로 사용합니다.

```json
{
  "playerId": "...",
  "items": [
    {
      "entryId": "...",
      "reason": "heroUpgrade",
      "balanceBefore": 100,
      "amount": -10,
      "balanceAfter": 90,
      "referenceId": "upgrade-001",
      "occurredAtUtc": "2026-07-11T00:00:00Z"
    }
  ],
  "nextCursor": "..."
}
```

잘못된 `pageSize`나 임의로 만든 커서는 DB 조회 전에 `400 ProblemDetails`로 거부됩니다.

## 5. 왜 커서 페이지네이션인가?

페이지 번호와 `OFFSET`은 원장 데이터가 계속 추가될 때 같은 행이 다시 보이거나 일부 행을 건너뛸 수 있고, 오래된 페이지로 갈수록 DB가 많은 행을 버려야 합니다.

현재 구현은 `(occurred_at_utc, entry_id)`를 다음 위치로 사용하는 키셋 페이지네이션입니다. 동일 시각의 여러 행도 `entry_id`로 안정적으로 순서를 결정합니다. API 커서는 이 내부 위치를 Base64 URL 형식으로 감싸 클라이언트가 DB 구조를 알 필요가 없게 합니다.

PostgreSQL에는 `(player_id, occurred_at_utc, entry_id)` 인덱스를 두어 플레이어 필터와 최신순 커서 조회를 지원합니다.

## 6. 요청 제한과 감사 범위

관리자 조회는 관리자 `sub` 기준 합산 120회/분으로 제한됩니다. 제한 초과 시 일반 API와 같은 429 `ProblemDetails`와 `Retry-After`를 반환합니다.

현재는 관리자 조회 행동 자체를 별도 감사 테이블에 저장하지 않습니다. 실제 개인정보·결제 데이터를 다루기 전에는 관리자 조회 감사 로그, 역할 세분화, MFA, 사유나 작업 티켓 연결, 민감 필드 마스킹과 토큰 폐기가 추가되어야 합니다.

## 7. 테스트 기준

- 토큰 없는 요청이 401인지 검증합니다.
- 정상 게스트 JWT가 403인지 검증합니다.
- 서명된 관리자 JWT로 지정 플레이어 상태를 읽는지 검증합니다.
- 원장 첫 페이지와 다음 커서 페이지가 중복 없이 이어지는지 검증합니다.
- 잘못된 커서가 400인지 검증합니다.
- 실제 PostgreSQL에서 플레이어 필터와 키셋 조건이 SQL로 동작하는지 검증합니다.
