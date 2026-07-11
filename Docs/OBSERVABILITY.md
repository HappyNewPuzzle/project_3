# 로깅, 메트릭과 예외 처리

이 문서는 서버가 운영 중 문제를 추적할 수 있도록 어떤 로그와 오류 응답을 남기는지 설명합니다.

## 1. 목적

방치형 게임 서버는 재화, 성장, 진행 상태를 서버가 권위 있게 판단합니다. 그래서 실패 상황이 생겼을 때 클라이언트에 내부 정보를 노출하지 않으면서도 서버 로그에서는 원인을 추적할 수 있어야 합니다.

## 2. 전역 예외 처리

예상 가능한 실패는 각 Endpoint가 직접 처리합니다.

- 멱등 키 누락: `400 ProblemDetails`
- 플레이어 상태 없음: `404 ProblemDetails`
- 멱등 키 충돌: `409 ProblemDetails`
- 저장 충돌 재시도 초과: `503 ProblemDetails`

예상하지 못한 예외는 `GlobalExceptionHandler`가 마지막 방어선으로 처리합니다.

```json
{
  "title": "An unexpected server error occurred.",
  "status": 500,
  "detail": "Contact support with the traceId if the problem persists.",
  "traceId": "..."
}
```

Development 환경에서는 학습과 디버깅 편의를 위해 `detail`에 예외 메시지를 포함합니다. 운영 환경에서는 내부 구현이 새지 않도록 추적 ID 안내만 반환합니다.

## 3. Trace ID

`traceId`는 클라이언트 오류 화면, 서버 로그, 테스트 실패를 연결하는 실마리입니다.

모든 HTTP 응답은 `X-Trace-Id` 헤더를 포함합니다. 오류 응답은 Body의 `traceId`도 제공합니다. Unity는 요청 실패 시 상태 코드, `X-Trace-Id`와 클라이언트 발생 시각을 함께 기록하면 서버의 같은 요청 로그를 찾을 수 있습니다.

## 4. 요청 로깅

서버는 ASP.NET Core `HttpLogging`과 관측성 미들웨어로 다음 구조화 필드를 기록합니다.

- HTTP Method
- Route Template
- Response Status Code
- Duration
- Trace ID
- 인증된 Player ID 또는 `anonymous`

요청 Body, 응답 Body, Authorization 헤더와 멱등 키는 기록하지 않습니다. Player ID는 계정 문의 연결을 위한 가명 식별자이며 로그 접근 권한과 보존 기간을 제한해야 합니다. 메트릭에는 Player ID를 태그로 넣지 않아 시계열 폭증을 막습니다.

## 5. API 메트릭

`IdleGuild.Api` Meter가 다음 값을 노출합니다.

| Instrument | 종류 | 의미 |
| --- | --- | --- |
| `idleguild.api.requests` | Counter | 완료된 HTTP 요청 수 |
| `idleguild.api.errors` | Counter | 상태 코드 500 이상 응답 수 |
| `idleguild.api.request.duration` | Histogram | HTTP 응답 시간(ms) |

태그는 `http.request.method`, `http.route`, `http.response.status_class`만 사용합니다. 실제 Path 대신 `/api/v1/stages/{stage}/challenge` 같은 Route Template을 사용해 플레이어·장비·구매 ID가 시계열을 무한히 늘리지 않게 합니다.

현재 서버는 .NET `Meter`를 생성하는 단계까지 구현했습니다. 운영 배포에서는 OpenTelemetry 또는 관리형 APM exporter를 연결해 Prometheus·Grafana 같은 외부 시스템으로 전송합니다. exporter 장애가 게임 요청 실패로 이어져서는 안 됩니다.

## 6. 우선 관찰할 신호

- 요청량: API·메서드별 초당 요청 수와 평시 대비 급증
- 오류율: 5xx 비율, 429 비율, 409·503의 지속 증가
- 지연 시간: 평균보다 p95·p99를 중심으로 확인
- 준비 상태: `/ready` 실패 지속 시간과 PostgreSQL 연결 오류
- 재화: 시간대별 방치 보상, 스테이지 보상, 상점 지급 총량과 플레이어별 상위값

초기 경보 후보는 5분간 5xx 비율 1% 초과, p95가 1초 초과, readiness 연속 실패입니다. 실제 임계값은 부하 테스트와 운영 기준선을 수집한 뒤 조정합니다.

재화 이상 탐지는 `gold_ledger_entries`를 기준으로 사유별 지급량을 집계합니다. 상점 지급 급증, 동일 플레이어의 비정상적으로 많은 신규 멱등 키, 전체 방치 보상의 평시 대비 급증을 조사 대상으로 삼되 자동 회수는 하지 않습니다.

## 7. 장애 조사 순서

1. 대시보드에서 영향 Route, 상태 코드와 시작 시각을 찾습니다.
2. `X-Trace-Id`로 한 요청의 구조화 로그와 예외 로그를 연결합니다.
3. Player ID와 원장 참조 ID로 상태 변경 전후를 확인합니다.
4. `/ready`, PostgreSQL 연결, 배포 버전과 Rate Limit 상태를 비교합니다.
5. 원인을 수정한 뒤 오류율·지연 시간·재화 집계가 기준선으로 복구됐는지 확인합니다.

## 8. 구현 위치

- `src/IdleGuild.Api/ErrorHandling/GlobalExceptionHandler.cs`: 처리되지 않은 예외를 500 `ProblemDetails`로 변환합니다.
- `src/IdleGuild.Api/Program.cs`: `AddExceptionHandler`, `AddProblemDetails`, `AddHttpLogging`, `UseExceptionHandler`, `UseHttpLogging`을 연결합니다.
- `src/IdleGuild.Api/Observability/ApiTelemetry.cs`: Counter와 Histogram 이름을 정의합니다.
- `src/IdleGuild.Api/Observability/ApiObservabilityMiddleware.cs`: Trace 헤더, 낮은 카디널리티 태그와 구조화 완료 로그를 생성합니다.
- `tests/IdleGuild.Api.Tests/ErrorHandlingTests.cs`: 강제로 예외를 발생시켜 500 `ProblemDetails`와 `traceId`를 검증합니다.
- `tests/IdleGuild.Api.Tests/ObservabilityTests.cs`: 요청 수와 응답 시간 측정을 검증합니다.

## 9. Health Check 운영 신호

`/health`는 API 프로세스 생존, `/ready`는 PostgreSQL 연결 준비 상태를 나타냅니다. DB 장애 때 `/ready`는 503이지만 `/health`는 200을 유지해 트래픽 차단과 프로세스 재시작 판단을 분리합니다.

Health Check의 외부 응답에는 연결 문자열이나 예외 메시지를 포함하지 않습니다. 상세 장애 원인은 서버 로그와 향후 Metric에서 확인합니다. 자세한 사용 기준은 [Liveness와 Readiness Health Check](HEALTH_CHECKS.md)에 정리합니다.
