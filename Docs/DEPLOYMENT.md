# 배포와 운영 설정

이 문서는 로컬 개발을 넘어 실제 배포 환경에서 확인해야 할 설정과 운영 체크리스트를 정리합니다.

현재 프로젝트는 포트폴리오용 서버 MVP이므로 특정 클라우드에 종속된 배포 스크립트까지 포함하지 않고, 어디에 배포하더라도 필요한 공통 기준을 먼저 정리합니다.

## 1. 배포 구성 개요

```text
Client 또는 API Tester
    |
    | HTTPS
    v
IdleGuild.Api
    |
    | Npgsql / EF Core
    v
PostgreSQL
```

운영 환경에서는 API 서버와 PostgreSQL을 분리하고, 연결 문자열과 JWT 서명 키는 환경 변수 또는 비밀 관리 도구로 주입합니다.

## 2. 필수 환경 변수

| 이름 | 예시 | 설명 |
| --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` | 실행 환경입니다. 운영에서는 `Production`을 사용합니다. |
| `ConnectionStrings__GameDatabase` | `Host=...;Port=5432;Database=...;Username=...;Password=...` | PostgreSQL 연결 문자열입니다. |
| `Jwt__Issuer` | `IdleGuild.Api` | 토큰 발급자입니다. |
| `Jwt__Audience` | `IdleGuild.Client` | 토큰 대상입니다. |
| `Jwt__SigningKey` | 32바이트 이상의 임의 비밀값 | HMAC SHA-256 JWT 서명 키입니다. |
| `Jwt__AccessTokenLifetimeMinutes` | `1440` | 게스트 액세스 토큰 유효 시간입니다. |

`Jwt__SigningKey`는 저장소에 커밋하지 않습니다. 운영 환경에서는 최소 32 UTF-8 바이트 이상의 충분히 긴 무작위 값을 사용합니다.

## 3. 데이터베이스 Migration

배포 전 또는 배포 파이프라인에서 EF Core Migration을 적용해야 합니다.

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update `
  --project src/IdleGuild.Infrastructure `
  --startup-project src/IdleGuild.Infrastructure
```

운영에서는 다음 원칙을 권장합니다.

- Migration 적용 권한과 API 실행 권한을 분리합니다.
- 배포 전 DB 백업 또는 롤백 계획을 준비합니다.
- Migration SQL을 사전에 검토합니다.

## 4. 운영 환경에서 바뀌는 동작

Development 환경:

- OpenAPI JSON과 Swagger UI를 노출합니다.
- 500 오류 `detail`에 예외 메시지를 포함합니다.

Production 환경:

- Swagger UI를 노출하지 않습니다.
- 자리표시자 JWT 서명 키를 사용하면 서버 시작을 거부합니다.
- 500 오류 응답은 내부 예외 메시지 대신 `traceId` 중심 안내만 제공합니다.

## 5. Health Check

API 생존 확인은 다음 Endpoint를 사용합니다.

```http
GET /health
```

현재 `/health`는 API 프로세스 생존 여부를 확인합니다. DB 연결까지 포함한 readiness check는 향후 배포 자동화 단계에서 확장할 수 있습니다.

## 6. 로그 기준

요청 로그는 다음 값만 남깁니다.

- HTTP Method
- Request Path
- Response Status Code
- Duration

Authorization 헤더, JWT, 요청 Body, 응답 Body는 기록하지 않습니다. 토큰과 플레이어 데이터가 로그에 남는 것을 막기 위한 선택입니다.

처리되지 않은 예외는 서버 로그에 기록되고 클라이언트에는 `traceId`가 포함된 500 `ProblemDetails`가 반환됩니다.

## 7. 배포 전 체크리스트

- [ ] `ASPNETCORE_ENVIRONMENT=Production` 설정
- [ ] 운영 PostgreSQL 연결 문자열 설정
- [ ] `Jwt__SigningKey`를 32바이트 이상 비밀값으로 교체
- [ ] `Jwt__Issuer`, `Jwt__Audience` 확인
- [ ] EF Core Migration 적용
- [ ] `/health` 응답 확인
- [ ] 게스트 생성 API 확인
- [ ] 보호 API가 토큰 없이 401을 반환하는지 확인
- [ ] 오류 응답에 내부 비밀값이 포함되지 않는지 확인

## 8. 현재 의도적으로 남긴 제한

- 운영용 CI/CD 파이프라인은 아직 포함하지 않았습니다.
- 컨테이너 이미지 빌드 파일은 배포 대상이 정해진 뒤 추가합니다.
- DB readiness check는 아직 `/health`에 포함하지 않았습니다.
- 게스트 인증은 학습용 MVP 기준이며, 공개 서비스에서는 OIDC/OAuth 같은 외부 인증 제공자 연동을 권장합니다.

이 제한은 숨겨진 결함이 아니라 MVP 경계를 명확히 하기 위한 선택입니다. 이후 클라우드 배포 Step에서 하나씩 확장할 수 있습니다.
