# API 컨테이너 빌드와 실행

이 문서는 Hardening Step 4에서 추가한 API Docker 이미지, PostgreSQL과 함께 실행하는 방법, Migration 분리 원칙과 운영 보안 설정을 설명합니다.

## 1. 만들어진 실행 단위

| 파일 | 역할 |
| --- | --- |
| `Dockerfile` | API를 Release로 게시하고 ASP.NET Runtime 이미지로 실행 |
| `.dockerignore` | Git, 비밀값, 테스트, Unity와 빌드 결과를 Build Context에서 제외 |
| `compose.yaml` | 기존 로컬 PostgreSQL 서비스 |
| `compose.api.yaml` | PostgreSQL 위에 API 컨테이너를 추가하는 Compose 파일 |

기존 `docker compose up postgres` 개발 흐름은 그대로 유지됩니다. API까지 컨테이너로 실행할 때만 두 Compose 파일을 함께 사용합니다.

## 2. 다단계 Dockerfile

빌드는 다음 두 단계로 나뉩니다.

```text
dotnet/sdk:10.0-alpine
    restore → Release publish
              |
              v
dotnet/aspnet:10.0-alpine
    게시 결과만 복사 → API 실행
```

최종 이미지에는 SDK, 소스 코드, 테스트 프로젝트와 Unity 프로젝트가 포함되지 않습니다. NuGet 복원 전에 프로젝트 파일만 먼저 복사해 소스 코드만 바뀐 빌드에서는 복원 레이어를 재사용합니다.

## 3. 이미지 빌드

저장소 루트에서 다음 명령을 실행합니다.

```powershell
docker build --tag idle-guild-api:local .
```

빌드는 `IdleGuild.Api`와 참조 프로젝트를 Release로 게시합니다. 빌드 인수나 이미지 레이어에 DB 비밀번호와 JWT 서명 키를 넣지 않습니다. 비밀값은 컨테이너 실행 시 환경 변수로만 주입합니다.

## 4. 로컬 컨테이너 실행 순서

### 4-1. 환경 파일 준비

```powershell
Copy-Item .env.example .env
```

`.env`의 `POSTGRES_PASSWORD`와 `JWT_SIGNING_KEY`를 로컬 전용 값으로 교체합니다. `.env`는 Git과 Docker Build Context에서 제외됩니다.

### 4-2. PostgreSQL 시작

```powershell
docker compose --env-file .env up -d --wait postgres
```

### 4-3. Migration 별도 적용

호스트에서 PostgreSQL에 연결할 수 있는 연결 문자열을 설정한 뒤 Migration을 적용합니다.

```powershell
$env:ConnectionStrings__GameDatabase = "Host=localhost;Port=5432;Database=idleguild;Username=idleguild;Password=replace_with_local_password"

dotnet tool restore
dotnet tool run dotnet-ef database update `
  --project src/IdleGuild.Infrastructure `
  --startup-project src/IdleGuild.Infrastructure
```

### 4-4. API 빌드와 시작

```powershell
docker compose --env-file .env `
  -f compose.yaml `
  -f compose.api.yaml `
  up -d --build api
```

API는 컨테이너 내부 8080 포트를 사용하고 기본적으로 호스트 `http://localhost:5219`에 연결됩니다.

### 4-5. 동작 확인

```powershell
Invoke-RestMethod http://localhost:5219/health
Invoke-RestMethod -Method Post http://localhost:5219/api/v1/accounts/guest
```

### 4-6. 컨테이너 정지

```powershell
docker compose --env-file .env `
  -f compose.yaml `
  -f compose.api.yaml `
  stop api postgres
```

`stop`은 PostgreSQL 데이터 볼륨을 삭제하지 않습니다.

## 5. Migration을 API 시작과 분리한 이유

API 시작 시 `Database.Migrate()`를 자동 호출하면 서버 인스턴스 여러 개가 동시에 시작될 때 모두 스키마 변경을 시도할 수 있습니다. 또한 API 실행 계정이 테이블 생성·변경 권한까지 가져야 합니다.

현재 배포 순서는 다음과 같습니다.

```text
DB 백업과 변경 검토
    ↓
Migration 전용 배포 작업 1회
    ↓
API 이미지 배포
    ↓
/health 확인
```

운영에서는 Migration 계정과 API 계정의 DB 권한을 분리하는 것이 권장됩니다. Migration 실패 시 API 새 버전을 시작하지 않고 이전 버전을 유지하도록 배포 파이프라인을 구성해야 합니다.

## 6. 컨테이너 보안 기본값

`compose.api.yaml`은 다음 실행 제한을 적용합니다.

- .NET Runtime 이미지의 UID 1654 비루트 사용자
- 읽기 전용 루트 파일시스템
- 임시 파일이 필요한 경우를 위한 메모리 `/tmp`
- Linux capability 전체 제거
- `no-new-privileges`
- Production 환경 고정
- Swagger와 개발용 상세 오류 비활성화

애플리케이션 로그는 표준 출력으로 기록하므로 컨테이너 파일에 로그를 저장할 쓰기 권한이 필요하지 않습니다.

## 7. 운영 이미지 태그와 비밀값

운영 이미지에는 변경 불가능한 커밋 SHA나 버전 태그를 사용합니다.

```powershell
docker build --tag registry.example.com/idle-guild-api:$env:GIT_COMMIT .
docker push registry.example.com/idle-guild-api:$env:GIT_COMMIT
```

`latest`만 사용하면 어떤 코드가 배포됐는지 추적하기 어렵습니다. 컨테이너 플랫폼에는 다음 값을 Secret으로 주입합니다.

- `ConnectionStrings__GameDatabase`
- `Jwt__SigningKey`

Issuer, Audience, 토큰 유효기간과 포트도 환경별 설정으로 관리합니다.

## 8. 현재 남은 범위

- 컨테이너 `/health`는 API 프로세스 생존만 확인합니다.
- PostgreSQL 준비 상태를 포함하는 `/ready`는 Hardening Step 5에서 추가합니다.
- 특정 Container Registry와 클라우드 배포는 아직 연결하지 않았습니다.
- 무중단 배포, 이미지 취약점 검사와 서명은 이후 CI/CD 단계에서 추가할 수 있습니다.
