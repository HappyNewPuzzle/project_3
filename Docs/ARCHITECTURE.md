# 서버 아키텍처

## 1. 설계 방향

첫 버전은 하나의 ASP.NET Core 애플리케이션으로 배포하는 **모듈형 모놀리스**로 구현합니다. 작은 포트폴리오 프로젝트에 불필요한 운영 복잡성을 만들지 않으면서도, 기능 간 경계를 코드에서 분명하게 보여 주기 위한 선택입니다.

## 2. 시스템 구성

```text
Unity Client
    |
    | HTTPS / JSON
    v
ASP.NET Core Web API
    |- Accounts
    |- Game State
    |- Economy
    |- Progression
    `- Battles
    |
    | EF Core
    v
PostgreSQL
```

## 3. 책임 분리

- API: 요청 형식 검증, 인증 정보 전달, HTTP 응답 변환
- Application: 유스케이스 실행과 트랜잭션 경계 관리
- Domain: 재화, 성장, 전투 규칙 계산
- Infrastructure: PostgreSQL, EF Core, 토큰과 서버 시각 구현

도메인 규칙은 ASP.NET Core나 데이터베이스에 직접 의존하지 않게 만들어 단위 테스트가 가능하도록 합니다.

## 4. 주요 모듈

| 모듈 | 책임 |
| --- | --- |
| Accounts | 게스트 계정 생성과 인증 |
| GameState | 클라이언트에 필요한 현재 상태 조회 |
| Economy | 골드 생산, 방치 보상, 재화 변경과 감사 원장 |
| Progression | 영웅 레벨과 강화 비용 |
| Battles | 스테이지 도전과 해금 판정 |
| Admin | 운영자 권한으로 플레이어 상태와 재화 원장 조회 |

### 현재 인증 경계

게스트 생성 API만 익명 접근을 허용합니다. 보호된 게임 API는 JWT의 서명, 발급자, 대상, 만료와 `sub` 플레이어 ID를 검증합니다. 클라이언트가 플레이어 ID를 요청 경로나 Body로 지정하지 않고 서버가 검증된 `sub`만 사용해 사용자별 데이터 접근을 제한합니다.

## 5. 데이터 원칙

- 모든 영속 시각은 UTC로 저장한다.
- 재화와 레벨은 음수가 될 수 없도록 애플리케이션과 DB 제약조건을 함께 사용한다.
- 재화 변경은 원인과 결과를 추적할 수 있도록 로그를 남긴다.
- 동시 변경 충돌은 낙관적 동시성 제어로 감지한다.
- 스키마 변경은 EF Core Migration으로 이력 관리한다.

### 현재 저장 모델

`player_game_states` 테이블은 플레이어 상태와 1/100 골드 생산 잔여값을 저장하고 PostgreSQL의 `xmin`을 동시성 토큰으로 사용합니다. `idle_reward_claim_receipts`, `hero_upgrade_receipts`, `stage_challenge_receipts`는 플레이어와 멱등 키별 최초 판정 결과를 저장합니다. `gold_ledger_entries`는 실제 골드 변경의 전후 잔액, 증감량, 사유와 참조 키를 저장합니다. 상태·영수증·원장은 같은 작업 단위로 원자적으로 반영되며, 동시 수정이나 유일 키 충돌이 발생하면 Application 계층이 최신 상태를 다시 읽어 최대 3회 시도합니다.

골드 원장의 상세 불변식과 기록 대상은 [골드 변경 이력 원장](GOLD_LEDGER.md)에 정리합니다.

### 관리자 읽기 경계

`/api/v1/admin` 경로는 서명된 JWT의 `account_type=admin` Claim을 추가로 요구합니다. 일반 게스트는 403으로 차단되며 관리자 API는 현재 상태 변경 명령을 제공하지 않습니다. 골드 원장은 최신순 키셋 페이지로 조회하고 PostgreSQL 복합 인덱스로 뒷받침합니다. 자세한 내용은 [관리자 조회 API](ADMIN_API.md)에 정리합니다.

## 6. API 초안

요청·응답 모델은 기능별 DTO로 유지하고, 일반 오류는 `ProblemDetails`로 반환합니다. 게임 규칙상 실패한 판정은 클라이언트가 그대로 표시하고 재시도 정책을 세울 수 있도록 기능 응답 DTO의 `outcome`으로 표현합니다.

| Method | Path | 목적 |
| --- | --- | --- |
| `POST` | `/api/v1/accounts/guest` | 게스트 계정 생성 |
| `GET` | `/api/v1/game-state` | 현재 게임 상태 조회 |
| `POST` | `/api/v1/rewards/idle/claim` | 방치 보상 수령 |
| `POST` | `/api/v1/heroes/main/upgrade` | 주 영웅 강화 |
| `POST` | `/api/v1/stages/{stage}/challenge` | 스테이지 도전 |

자세한 실패 응답 규칙은 [API 오류 계약](API_ERRORS.md)에 정리합니다.

## 7. 품질 기준

- 핵심 계산 규칙은 단위 테스트로 검증한다.
- PostgreSQL이 필요한 저장 동작은 통합 테스트로 검증한다.
- 공개 API는 OpenAPI 문서에서 실행할 수 있어야 한다.
- 비밀값은 저장소에 커밋하지 않고 환경 변수로 주입한다.
- 각 Step은 실행 또는 테스트 가능한 상태에서 커밋한다.

## 8. 운영 관측성

서버는 요청 메서드, 경로, 응답 상태 코드, 소요 시간만 로그로 남깁니다. Authorization 헤더나 Body는 기록하지 않아 토큰과 사용자 데이터가 로그에 섞이지 않게 합니다.

처리되지 않은 예외는 전역 예외 처리기가 서버 로그에 남기고 클라이언트에는 `traceId`가 포함된 500 `ProblemDetails`를 반환합니다. 자세한 내용은 [로깅과 예외 처리](OBSERVABILITY.md)에 정리합니다.

## 9. 요청 남용 방어

익명 게스트 생성은 연결 IP, 인증된 상태 변경은 JWT 플레이어 ID를 기준으로 1분 고정 윈도우 Rate Limit을 적용합니다. 제한 요청은 Application과 DB에 도달하기 전에 429로 종료됩니다.

현재 카운터는 단일 API 프로세스 메모리에 있으며, 다중 인스턴스에서는 API Gateway나 Redis 기반 공유 제한으로 확장해야 합니다. 상세 정책은 [API 요청 속도 제한](RATE_LIMITING.md)에 정리합니다.
