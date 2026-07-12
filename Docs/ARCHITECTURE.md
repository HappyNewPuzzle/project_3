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
| Equipment | 장비 마스터, 보유 인스턴스, 장착과 전투력 보너스 |
| Shop | 서버 상품 카탈로그, 모의 구매와 구매 영수증 |

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

## 6. 현재 API

요청·응답 모델은 기능별 DTO로 유지하고, 일반 오류는 `ProblemDetails`로 반환합니다. 게임 규칙상 실패한 판정은 클라이언트가 그대로 표시하고 재시도 정책을 세울 수 있도록 기능 응답 DTO의 `outcome`으로 표현합니다.

| Method | Path | 목적 |
| --- | --- | --- |
| `POST` | `/api/v1/accounts/guest` | 게스트 계정 생성 |
| `GET` | `/api/v1/game-state` | 현재 게임 상태 조회 |
| `POST` | `/api/v1/rewards/idle/claim` | 방치 보상 수령 |
| `POST` | `/api/v1/heroes/main/upgrade` | 주 영웅 강화 |
| `POST` | `/api/v1/stages/{stage}/challenge` | 스테이지 도전 |
| `GET` | `/api/v1/equipment` | 보유 장비와 장착 보너스 조회 |
| `PUT` | `/api/v1/equipment/{equipmentId}/equipped` | 장비 장착 또는 해제 |
| `GET` | `/api/v1/shop/products` | 모의 상품 카탈로그 조회 |
| `POST` | `/api/v1/shop/products/{productId}/purchase` | 멱등 모의 구매 |
| `GET` | `/api/v1/shop/purchases` | 구매 이력 조회 |
| `GET` | `/api/v1/admin/players/{playerId}` | 관리자 플레이어 상태 조회 |
| `GET` | `/api/v1/admin/players/{playerId}/gold-ledger` | 관리자 골드 원장 조회 |

자세한 실패 응답 규칙은 [API 오류 계약](API_ERRORS.md)에 정리합니다.

## 7. 품질 기준

- 핵심 계산 규칙은 단위 테스트로 검증한다.
- PostgreSQL이 필요한 저장 동작은 통합 테스트로 검증한다.
- 공개 API는 OpenAPI 문서에서 실행할 수 있어야 한다.
- 비밀값은 저장소에 커밋하지 않고 환경 변수로 주입한다.
- 각 Step은 실행 또는 테스트 가능한 상태에서 커밋한다.

## 8. 운영 관측성

서버는 요청 메서드, Route Template, 응답 상태 코드, 소요 시간, Trace ID와 인증 플레이어 ID를 구조화 로그로 남깁니다. Authorization 헤더, Body와 멱등 키는 기록하지 않아 토큰과 요청 데이터가 로그에 섞이지 않게 합니다.

처리되지 않은 예외는 전역 예외 처리기가 서버 로그에 남기고 클라이언트에는 `traceId`가 포함된 500 `ProblemDetails`를 반환합니다. 모든 응답은 `X-Trace-Id` 헤더를 포함하고, 요청 수·5xx·응답 시간은 .NET Meter로 측정합니다. 자세한 내용은 [로깅, 메트릭과 예외 처리](OBSERVABILITY.md)에 정리합니다.

## 9. 요청 남용 방어

익명 게스트 생성은 연결 IP, 인증된 상태 변경은 JWT 플레이어 ID를 기준으로 1분 고정 윈도우 Rate Limit을 적용합니다. 제한 요청은 Application과 DB에 도달하기 전에 429로 종료됩니다.

현재 카운터는 단일 API 프로세스 메모리에 있으며, 다중 인스턴스에서는 API Gateway나 Redis 기반 공유 제한으로 확장해야 합니다. 상세 정책은 [API 요청 속도 제한](RATE_LIMITING.md)에 정리합니다.

## 10. 배포 경계

API는 .NET SDK 빌드 단계와 ASP.NET Runtime 실행 단계를 분리한 Linux 컨테이너로 배포할 수 있습니다. 최종 컨테이너는 비루트 사용자와 읽기 전용 파일시스템으로 실행합니다.

DB Migration은 API 프로세스 시작 책임에 포함하지 않습니다. 배포 작업이 Migration을 한 번 적용한 뒤 API 이미지를 시작해 스키마 변경 권한과 런타임 권한을 분리합니다. 자세한 내용은 [API 컨테이너 빌드와 실행](CONTAINER_DEPLOYMENT.md)에 정리합니다.

## 11. 생존과 준비 상태

`/health`는 API 프로세스만 검사하고 `/ready`는 EF Core를 통해 PostgreSQL 실제 연결을 검사합니다. DB 장애 중에는 API 프로세스를 재시작하지 않고 readiness만 503으로 바꿔 새 트래픽을 차단합니다. 자세한 내용은 [Liveness와 Readiness Health Check](HEALTH_CHECKS.md)에 정리합니다.

## 12. 장비 경계

Equipment 모듈은 코드의 장비 마스터와 PostgreSQL의 플레이어 보유 장비를 분리합니다. 장착 변경은 Application이 소유권과 슬롯을 검사하고, PostgreSQL 부분 유일 인덱스가 플레이어별 같은 슬롯의 중복 장착을 최종 차단합니다. 장착 장비 보너스는 게임 상태 조회와 스테이지 판정에 동일하게 반영합니다.

공개 API는 `GET /api/v1/equipment`와 `PUT /api/v1/equipment/{equipmentId}/equipped`이며 변경 API는 멱등 키와 영수증을 사용합니다. 자세한 규칙은 [장비 시스템](EQUIPMENT_SYSTEM.md)에 정리합니다.

## 13. 모의 상점 경계

Shop 모듈은 서버 상품 카탈로그, 구매 유스케이스와 영구 구매 영수증을 분리합니다. 구매 시 골드 상태, `shop_purchase_receipts`, `ShopPurchase` 골드 원장을 같은 PostgreSQL 트랜잭션으로 저장합니다. 실제 플랫폼 결제는 포함하지 않으며 차이는 [모의 상점과 구매 이력](MOCK_SHOP.md)에 정리합니다.
