# 서버 MVP 완료 정리

이 문서는 Idle Guild 서버 MVP가 어떤 기준으로 완료되었는지, 현재 포함된 기능과 의도적으로 제외한 범위를 정리합니다.

## 1. 완료 선언

서버 MVP는 다음 기준으로 완료 상태입니다.

- 서버가 플레이어의 시간, 재화, 성장, 스테이지 결과를 직접 계산합니다.
- PostgreSQL에 플레이어 상태와 멱등 영수증을 저장합니다.
- 게스트 JWT로 사용자별 데이터 접근을 제한합니다.
- 방치 보상, 영웅 강화, 스테이지 진행이 하나의 핵심 루프로 연결됩니다.
- 중복 요청과 동시 요청으로 재화가 중복 지급되거나 차감되지 않도록 방어합니다.
- Unity 클라이언트가 사용할 API 계약, 오류 계약, 데모 시나리오가 문서화되어 있습니다.
- 자동 테스트로 Domain, Application, API, Infrastructure 계층을 검증합니다.

## 2. 서버 MVP에 포함된 기능

| 영역 | 포함된 내용 |
| --- | --- |
| 계정 | 게스트 계정 생성, JWT 발급, 인증된 사용자 상태 접근 |
| 게임 상태 | 골드, 영웅 레벨, 최고 스테이지, 생산 보너스 조회 |
| 방치 보상 | 서버 UTC 기준 누적 시간 계산, 8시간 상한, 멱등 수령 |
| 영웅 성장 | 서버 권위 강화 비용 계산, 골드 차감, 레벨 증가, 실패 멱등성 |
| 스테이지 진행 | 결정론적 전투력 판정, 스테이지 해금, 생산 보너스 적용 |
| 저장소 | PostgreSQL, EF Core Migration, 낙관적 동시성 제어 |
| API 계약 | OpenAPI, 기능별 응답 DTO, `ProblemDetails` 오류 응답 |
| 운영 기본기 | Health Check, 요청 로깅, 전역 예외 처리, `traceId` |
| 문서 | 설계, 구조, 의사결정, 데모, 배포 체크리스트 |

## 3. 핵심 플레이 루프

```text
POST /api/v1/accounts/guest
    ↓
GET /api/v1/game-state
    ↓
POST /api/v1/rewards/idle/claim
    ↓
POST /api/v1/heroes/main/upgrade
    ↓
POST /api/v1/stages/{stage}/challenge
    ↓
GET /api/v1/game-state
```

이 흐름을 통해 플레이어는 서버가 계산한 보상을 받고, 골드를 사용해 영웅을 강화하고, 더 높은 스테이지를 열어 생산 보너스를 얻습니다.

## 4. 검증 기준

서버 MVP 완료는 다음 테스트 범위로 확인합니다.

| 테스트 계층 | 검증 내용 |
| --- | --- |
| Domain Tests | 순수 게임 규칙, 계산 공식, 상태 변경 규칙 |
| Application Tests | 유스케이스 처리 순서, 멱등 재시도, 저장 충돌 재시도 |
| API Tests | 인증, HTTP 상태 코드, 응답 DTO, `ProblemDetails`, OpenAPI |
| Infrastructure Tests | PostgreSQL 저장 왕복, Migration, 동시성, 영수증 무결성 |

현재 테스트 수는 다음과 같습니다.

- Domain: 25개
- Application: 10개
- API: 21개
- Infrastructure: 6개
- 총 62개

PostgreSQL 통합 테스트는 Docker Desktop 또는 외부 테스트 DB가 필요합니다.

## 5. 의도적으로 제외한 범위

아래 항목은 현재 서버 MVP에서 제외했습니다.

| 제외 항목 | 제외 이유 |
| --- | --- |
| Unity 클라이언트 | 서버 구조 학습과 포트폴리오 서버 MVP를 먼저 완성하기 위해 별도 Step으로 분리했습니다. |
| 소셜 로그인/OIDC | 게스트 JWT만으로 사용자 격리와 서버 권위 구조를 검증할 수 있기 때문입니다. |
| 결제/광고 | 재화 서버의 기본 무결성과 멱등성 학습 범위를 벗어납니다. |
| 실시간 전투 | MVP에서는 결정론적 스테이지 판정으로 성장 루프를 검증하는 것이 더 적합합니다. |
| 랭킹/PvP | 동기화, 부정행위 방지, 시즌 운영 등 별도 설계가 필요합니다. |
| Redis/메시지 큐 | 현재 규모에서는 PostgreSQL과 단일 API 서버로 학습 목표를 달성할 수 있습니다. |
| 클라우드별 CI/CD | 배포 대상이 정해진 뒤 Dockerfile, pipeline, readiness check를 추가하는 것이 좋습니다. |

이 제외 항목은 미완성이 아니라 MVP 경계를 명확히 하기 위한 선택입니다.

## 6. Unity 클라이언트로 넘어가기 전 확인할 것

Unity 클라이언트 세션을 시작하기 전에 다음 내용을 기준으로 서버를 사용하면 됩니다.

- 기본 서버 주소: `http://localhost:5219`
- 인증 방식: `Authorization: Bearer <accessToken>`
- 상태 변경 API는 `Idempotency-Key` 헤더 필수
- 오류 응답은 `ProblemDetails` 기준으로 처리
- 게임 규칙상 실패는 기능 응답 DTO의 `outcome`으로 처리
- 핵심 데모 흐름은 `Docs/DEMO_SCENARIO.md` 참고

## 7. 다음 확장 후보

서버 MVP 이후 선택할 수 있는 확장 후보입니다.

1. Unity 클라이언트 구현
2. Dockerfile과 배포 자동화
3. DB readiness health check
4. 게스트 계정 연동 또는 외부 인증
5. 장비/스킬/퀘스트 같은 추가 성장 시스템
6. 랭킹, 시즌, PvP 같은 경쟁 콘텐츠

현재 권장 순서는 Unity 클라이언트를 먼저 붙여 API 계약과 사용자 흐름을 검증하는 것입니다.
