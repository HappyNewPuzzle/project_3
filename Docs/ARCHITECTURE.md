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
| Economy | 골드 생산, 방치 보상, 재화 변경 |
| Progression | 영웅 레벨과 강화 비용 |
| Battles | 스테이지 도전과 해금 판정 |

## 5. 데이터 원칙

- 모든 영속 시각은 UTC로 저장한다.
- 재화와 레벨은 음수가 될 수 없도록 애플리케이션과 DB 제약조건을 함께 사용한다.
- 재화 변경은 원인과 결과를 추적할 수 있도록 로그를 남긴다.
- 동시 변경 충돌은 낙관적 동시성 제어로 감지한다.
- 스키마 변경은 EF Core Migration으로 이력 관리한다.

### 현재 저장 모델

`player_game_states` 테이블은 플레이어 ID, 골드, 영웅 레벨, 최고 스테이지, 생성 시각과 마지막 방치 보상 정산 시각을 저장합니다. PostgreSQL의 `xmin` 시스템 열을 동시성 토큰으로 사용하고, 음수 골드나 1보다 작은 레벨·스테이지는 DB 제약조건으로도 차단합니다.

## 6. API 초안

구현 과정에서 요청·응답 모델과 오류 계약을 구체화합니다.

| Method | Path | 목적 |
| --- | --- | --- |
| `POST` | `/api/v1/accounts/guest` | 게스트 계정 생성 |
| `GET` | `/api/v1/game-state` | 현재 게임 상태 조회 |
| `POST` | `/api/v1/rewards/idle/claim` | 방치 보상 수령 |
| `POST` | `/api/v1/heroes/main/upgrade` | 주 영웅 강화 |
| `POST` | `/api/v1/stages/{stage}/challenge` | 스테이지 도전 |

## 7. 품질 기준

- 핵심 계산 규칙은 단위 테스트로 검증한다.
- PostgreSQL이 필요한 저장 동작은 통합 테스트로 검증한다.
- 공개 API는 OpenAPI 문서에서 실행할 수 있어야 한다.
- 비밀값은 저장소에 커밋하지 않고 환경 변수로 주입한다.
- 각 Step은 실행 또는 테스트 가능한 상태에서 커밋한다.
