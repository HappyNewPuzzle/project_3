# 데이터베이스

## 1. 구성

로컬 개발과 테스트는 PostgreSQL 18을 사용합니다.

- 애플리케이션 ORM: Entity Framework Core
- PostgreSQL 공급자: Npgsql
- 스키마 이력: EF Core Migration
- 로컬 DB: Docker Compose
- 통합 테스트 DB: Testcontainers

## 2. 데이터 흐름

```text
PlayerGameState
    |
    | EF Core configuration
    v
GameDbContext
    |
    | Npgsql
    v
PostgreSQL player_game_states
```

Domain 객체는 PostgreSQL을 알지 못합니다. 열 이름, 제약조건, `xmin` 같은 DB 세부사항은 Infrastructure의 EF 설정이 담당합니다.

## 3. player_game_states

| 열 | PostgreSQL 형식 | 역할 |
| --- | --- | --- |
| `player_id` | `uuid` | 플레이어 기본키 |
| `gold` | `bigint` | 보유 골드 |
| `hero_level` | `integer` | 주 영웅 레벨 |
| `highest_stage` | `integer` | 최고 도달 스테이지 |
| `created_at_utc` | `timestamp with time zone` | 생성 시각 |
| `last_idle_reward_claimed_at_utc` | `timestamp with time zone` | 마지막 방치 보상 정산 시각 |
| `xmin` | `xid` | PostgreSQL이 갱신하는 동시성 토큰 |

DB 체크 제약조건은 골드가 음수가 되거나 영웅 레벨과 최고 스테이지가 1보다 작아지는 것을 차단합니다.

## 4. Migration

Migration은 C# 모델 변경을 재현 가능한 DB 스키마 변경 이력으로 저장합니다.

```powershell
dotnet tool restore

dotnet tool run dotnet-ef migrations add MigrationName `
  --project src/IdleGuild.Infrastructure `
  --startup-project src/IdleGuild.Infrastructure `
  --context GameDbContext `
  --output-dir Persistence/Migrations
```

로컬 DB에 모든 Migration을 적용하려면 연결 문자열을 환경 변수로 제공한 후 다음 명령을 실행합니다.

```powershell
$env:ConnectionStrings__GameDatabase = "Host=localhost;Port=5432;Database=idleguild;Username=idleguild;Password=replace_with_local_password"

dotnet tool run dotnet-ef database update `
  --project src/IdleGuild.Infrastructure `
  --startup-project src/IdleGuild.Infrastructure
```

## 5. 통합 테스트

`PlayerGameStatePersistenceTests`는 다음 순서로 실제 PostgreSQL 동작을 검증합니다.

1. PostgreSQL 컨테이너를 시작한다.
2. 빈 DB에 모든 Migration을 적용한다.
3. 새로운 게임 상태를 저장한다.
4. 별도의 DbContext로 다시 조회한다.
5. 초기값, UTC 시각, `xmin` 버전을 확인한다.

Docker를 직접 제어할 수 없는 CI 환경에서는 `IDLEGUILD_TEST_POSTGRES_CONNECTION_STRING` 환경 변수로 준비된 테스트 DB를 지정할 수 있습니다.
