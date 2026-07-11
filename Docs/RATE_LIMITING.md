# API 요청 속도 제한

이 문서는 Hardening Step 2에서 추가한 Rate Limiting의 목적, 적용 범위, 429 오류 계약과 운영 확장 기준을 설명합니다.

## 1. 왜 필요한가?

멱등 키는 같은 요청이 여러 번 처리되어 재화가 중복 지급되는 것을 막습니다. 하지만 요청 자체를 무제한으로 보낼 수 있다면 다음 문제가 남습니다.

- 익명 게스트 계정을 대량 생성해 DB를 채울 수 있습니다.
- 보상·강화·스테이지 API를 자동화 도구가 계속 호출할 수 있습니다.
- 불필요한 인증, 조회, DB 부하가 정상 플레이에 영향을 줄 수 있습니다.

Rate Limiting은 요청 처리 전에 과도한 호출을 429로 거부해 서버 자원을 보호합니다.

## 2. 현재 정책

모든 정책은 1분 고정 윈도우이며 대기열은 사용하지 않습니다.

| 정책 | 분리 기준 | 허용량 | 적용 대상 |
| --- | --- | ---: | --- |
| `guest-account` | 연결 IP | 5회/분 | `POST /api/v1/accounts/guest` |
| `player-mutation` | JWT `sub` 플레이어 ID | 합산 30회/분 | 보상 수령, 영웅 강화, 스테이지 도전 |
| `admin-read` | 관리자 JWT `sub` | 합산 120회/분 | 플레이어 상태와 골드 원장 조회 |

게임 상태 조회, Health Check, 시스템 상태 조회는 현재 제한하지 않습니다. 화면 갱신과 서버 상태 확인이 게임 상태 변경 제한을 소모하지 않게 하기 위한 선택입니다.

인증되지 않은 상태로 변경 API를 반복 호출하면 플레이어 ID가 없으므로 연결 IP를 보조 분리 키로 사용합니다.

## 3. 처리 순서

플레이어별 제한은 검증된 JWT의 `sub`를 사용해야 하므로 인증 이후에 Rate Limiter가 실행됩니다.

```text
HTTP 요청
    ↓
JWT 인증
    ↓
Rate Limiting 분리 키 선택
    ↓
허용: Endpoint와 Application 실행
거부: 429 ProblemDetails 반환
```

제한된 요청은 Application Handler와 PostgreSQL 저장 단계까지 도달하지 않습니다. 따라서 제한 초과 요청은 기능 영수증이나 골드 원장을 생성하지 않습니다.

## 4. 429 오류 계약

제한을 초과하면 `application/problem+json` Body와 `Retry-After` 헤더를 반환합니다.

```json
{
  "type": "https://httpstatuses.com/429",
  "title": "Too many requests.",
  "status": 429,
  "detail": "Wait before retrying this operation.",
  "traceId": "...",
  "retryAfterSeconds": 42
}
```

Unity 클라이언트는 다음 원칙으로 처리합니다.

1. 같은 요청을 즉시 반복하지 않습니다.
2. `Retry-After` 또는 `retryAfterSeconds`만큼 기다립니다.
3. 상태 변경 요청을 다시 보낼 때 기존 `Idempotency-Key`를 유지합니다.
4. 버튼을 잠시 비활성화하거나 남은 대기 시간을 표시합니다.

## 5. 현재 운영 경계

현재 카운터는 ASP.NET Core API 프로세스 메모리에 있습니다. 서버 한 대에서는 올바르게 동작하지만 여러 API 인스턴스로 확장하면 인스턴스마다 별도 카운터를 갖습니다.

다중 인스턴스 운영에서는 다음 중 하나가 필요합니다.

- API Gateway 또는 Load Balancer의 공통 Rate Limit
- Redis 같은 공유 카운터 저장소
- 특정 플레이어 요청을 같은 인스턴스로 보내는 방식과 장애 정책

현재 IP는 직접 연결의 `RemoteIpAddress`를 사용합니다. Reverse Proxy 뒤에서 운영할 때는 신뢰할 프록시 범위를 명시한 Forwarded Headers 설정이 먼저 필요합니다. 외부 요청의 `X-Forwarded-For`를 검증 없이 신뢰해서는 안 됩니다.

Redis 도입 여부와 다중 인스턴스 장애 정책은 [Redis 도입 의사결정](REDIS_DECISION.md)에 정리합니다. 현재 단일 인스턴스에서는 메모리 제한기를 유지하며, 수평 확장 시 공유 Rate Limit을 첫 Redis 후보로 재검토합니다.

## 6. 테스트 기준

- 같은 IP의 게스트 생성이 5회까지 허용되고 다음 요청이 429인지 검증합니다.
- 429가 `application/problem+json`, `Retry-After`, `retryAfterSeconds`를 포함하는지 검증합니다.
- 한 플레이어가 상태 변경 한도를 모두 사용하면 다음 요청이 429인지 검증합니다.
- 같은 IP에서 생성된 다른 플레이어는 독립된 상태 변경 한도를 사용하는지 검증합니다.
