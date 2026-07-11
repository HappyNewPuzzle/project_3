# Liveness와 Readiness Health Check

이 문서는 Hardening Step 5에서 분리한 API 프로세스 생존 상태와 PostgreSQL 요청 처리 준비 상태를 설명합니다.

## 1. Endpoint 역할

| Endpoint | 검사 대상 | 성공 | 실패 의미 |
| --- | --- | --- | --- |
| `/health` | API 프로세스와 HTTP 파이프라인 | `200 Healthy` | 프로세스가 응답하지 못함 |
| `/ready` | PostgreSQL 실제 연결 | `200 Healthy` | `503 Unhealthy`, 트래픽 처리 준비 안 됨 |

두 Endpoint는 인증 없이 배포 플랫폼과 모니터링 도구가 호출할 수 있습니다. 기본 응답은 상세 연결 문자열이나 예외를 노출하지 않는 짧은 상태 문자열입니다.

## 2. 왜 분리하는가?

PostgreSQL이 잠시 재시작되거나 네트워크가 끊겨도 API 프로세스 자체는 정상일 수 있습니다. 이때 liveness가 DB까지 검사하면 컨테이너 플랫폼이 정상 API 프로세스를 반복 재시작해 장애를 더 키울 수 있습니다.

```text
API 프로세스 정상 + DB 장애
    |
    |-- /health = 200  → 프로세스 재시작 불필요
    `-- /ready  = 503  → 새 트래픽 전달 중지
```

DB가 복구되면 같은 API 프로세스의 `/ready`가 다시 200이 되고 트래픽을 받을 수 있습니다.

## 3. PostgreSQL 검사 방식

`PostgreSqlReadinessProbe`는 EF Core `Database.CanConnectAsync`로 실제 DB 연결 가능 여부를 확인합니다. 스키마를 변경하거나 데이터를 쓰지 않습니다.

API의 `PostgreSqlReadinessHealthCheck`가 probe 결과를 ASP.NET Core `HealthCheckResult`로 변환합니다. 검사는 최대 3초로 제한되어 DB 장애 중 Health 요청이 오래 쌓이지 않게 합니다.

## 4. 컨테이너와 배포 플랫폼

Docker 이미지의 `HEALTHCHECK`는 컨테이너 내부에서 `http://127.0.0.1:8080/ready`를 호출합니다. 따라서 API와 PostgreSQL이 모두 준비됐을 때 Docker 상태가 `healthy`가 됩니다.

Kubernetes 같은 플랫폼에서는 다음처럼 분리해서 사용합니다.

```text
livenessProbe  → /health
readinessProbe → /ready
```

Load Balancer의 트래픽 대상 확인에는 `/ready`를 사용하고, 단순 프로세스 모니터링에는 `/health`를 사용합니다.

## 5. 상태 코드 처리

- `/health` 200: API 프로세스가 살아 있습니다.
- `/ready` 200: DB 연결이 가능해 게임 요청을 처리할 수 있습니다.
- `/ready` 503: 새 요청을 보내지 않고 DB 복구를 기다립니다.
- 연결 타임아웃 또는 예외: 외부에는 503만 반환하고 상세 원인은 서버 Health Check 로그에서 확인합니다.

Health Check 503은 일반 게임 API의 `ProblemDetails` 오류 계약과 다릅니다. 배포 플랫폼이 소비하는 운영 신호이므로 기본 Health Check 텍스트 형식을 유지합니다.

## 6. 검증 기준

- 정상 probe에서 `/ready`가 200인지 API 테스트로 검증합니다.
- 실패 probe에서 `/ready`만 503이고 `/health`는 200인지 검증합니다.
- Testcontainers PostgreSQL에 실제 연결할 수 있는지 Infrastructure 테스트로 검증합니다.
- Production API 컨테이너가 `/ready`를 통해 Docker `healthy`가 되는지 확인합니다.
- PostgreSQL 중지 시 `/health=200`, `/ready=503`인지 확인합니다.
- PostgreSQL 재시작 후 `/ready=200`으로 복구되는지 확인합니다.
