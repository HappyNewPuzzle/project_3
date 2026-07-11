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
    |
    |-- 1:N gold_ledger_entries
    |-- 1:N idle_reward_claim_receipts
    |-- 1:N hero_upgrade_receipts
    `-- 1:N stage_challenge_receipts
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
| `idle_reward_remainder_hundredths` | `integer` | 다음 정산으로 이월할 1/100 골드 |
| `xmin` | `xid` | PostgreSQL이 갱신하는 동시성 토큰 |

DB 체크 제약조건은 골드가 음수가 되거나 영웅 레벨과 최고 스테이지가 1보다 작아지는 것을 차단합니다.

## 4. gold_ledger_entries

| 열 | PostgreSQL 형식 | 역할 |
| --- | --- | --- |
| `entry_id` | `uuid` | 원장 행 기본키 |
| `player_id` | `uuid` | 플레이어 ID와 외래키 |
| `reason` | `integer` | 골드 변경 사유 |
| `balance_before` | `bigint` | 변경 전 골드 |
| `amount` | `bigint` | 양수 지급 또는 음수 사용량 |
| `balance_after` | `bigint` | 변경 후 골드 |
| `reference_id` | `varchar(64)` | 기능 요청의 멱등 키 |
| `occurred_at_utc` | `timestamp with time zone` | 서버 처리 시각 |

DB 제약조건은 `balance_after = balance_before + amount`, 음수가 아닌 잔액, 0이 아닌 증감량을 검사합니다. `(player_id, reason, reference_id)` 유일 인덱스는 같은 기능 요청의 원장 중복을 차단합니다. 자세한 기록 규칙은 [골드 변경 이력 원장](GOLD_LEDGER.md)에 정리되어 있습니다.

관리자 최신순 페이지 조회에는 `(player_id, occurred_at_utc, entry_id)` 인덱스를 사용합니다. 처리 시각이 같은 행도 `entry_id`로 순서를 고정해 커서 페이지 사이의 중복과 누락을 방지합니다.

## 5. idle_reward_claim_receipts

| 열 | PostgreSQL 형식 | 역할 |
| --- | --- | --- |
| `player_id` | `uuid` | 플레이어 ID와 외래키 |
| `idempotency_key` | `varchar(64)` | 클라이언트 요청의 중복 방지 키 |
| `gold_awarded` | `bigint` | 실제 지급 골드 |
| `accumulated_seconds` | `integer` | 보상에 반영된 초 |
| `gold_balance_after` | `bigint` | 지급 직후 골드 잔액 |
| `remainder_hundredths` | `integer` | 지급 후 남은 1/100 골드 |
| `production_percent` | `integer` | 정산에 적용한 기준 대비 생산 배율 |
| `claimed_at_utc` | `timestamp with time zone` | 서버 정산 시각 |

`(player_id, idempotency_key)` 복합 기본키가 같은 요청의 영수증을 하나만 허용합니다. 영수증은 재시도 시 최초 결과를 그대로 반환하는 근거이며 플레이어 삭제 시 함께 삭제됩니다.

## 6. hero_upgrade_receipts

| 열 | PostgreSQL 형식 | 역할 |
| --- | --- | --- |
| `player_id` | `uuid` | 플레이어 ID와 외래키 |
| `idempotency_key` | `varchar(64)` | 강화 요청의 중복 방지 키 |
| `outcome` | `integer` | 성공, 골드 부족, 최대 레벨 판정 |
| `previous_level` | `integer` | 판정 전 영웅 레벨 |
| `hero_level_after` | `integer` | 판정 후 영웅 레벨 |
| `gold_cost` | `bigint` | 판정 당시 필요한 강화 비용 |
| `gold_balance_after` | `bigint` | 판정 직후 골드 잔액 |
| `processed_at_utc` | `timestamp with time zone` | 서버 처리 시각 |

복합 기본키는 같은 강화 키를 한 번만 저장합니다. 체크 제약조건은 결과별 레벨 관계, 음수가 아닌 골드, 최대 레벨을 DB에서도 검증합니다.

## 7. stage_challenge_receipts

| 열 | PostgreSQL 형식 | 역할 |
| --- | --- | --- |
| `player_id` | `uuid` | 플레이어 ID와 외래키 |
| `idempotency_key` | `varchar(64)` | 도전 요청 중복 방지 키 |
| `target_stage` | `integer` | 요청한 목표 스테이지 |
| `outcome` | `integer` | 성공 또는 실패 판정 |
| `previous_highest_stage` | `integer` | 판정 전 최고 스테이지 |
| `highest_stage_after` | `integer` | 판정 후 최고 스테이지 |
| `hero_power` | `integer` | 판정 당시 영웅 전투력 |
| `required_power` | `integer` | 목표 스테이지 요구 전투력 |
| `production_bonus_percent_after` | `integer` | 판정 후 생산 보너스 |
| `checkpoint_gold_awarded` | `bigint` | 성공 전 기존 배율 정산 골드 |
| `gold_balance_after` | `bigint` | 판정 직후 골드 |
| `processed_at_utc` | `timestamp with time zone` | 서버 판정 시각 |

복합 기본키가 같은 도전 키를 한 번만 저장합니다. 체크 제약조건은 결과별 스테이지 관계, 전투력 비교, 음수가 아닌 골드를 DB에서도 검증합니다.

## 8. Migration

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

## 9. 통합 테스트

`PlayerGameStatePersistenceTests`는 다음 순서로 실제 PostgreSQL 동작을 검증합니다.

1. PostgreSQL 컨테이너를 시작한다.
2. 빈 DB에 모든 Migration을 적용한다.
3. 새로운 게임 상태를 저장한다.
4. 별도의 DbContext로 다시 조회한다.
5. 초기값, UTC 시각, `xmin` 버전을 확인한다.

`IdleRewardConcurrencyTests`는 두 DbContext가 같은 키로 동시에 보상을 요청해도 영수증과 골드 원장이 각각 한 개만 남고 골드가 한 번만 지급되는지 검증합니다.

`HeroUpgradeConcurrencyTests`는 한 번의 비용만 가진 상태에서 동시 강화가 하나만 성공하고 차감 원장도 하나만 생기는지, 같은 실패 키에는 원장이 생기지 않는지 검증합니다.

`StageProgressionPersistenceTests`는 동시 스테이지 도전이 한 번만 진행되고 체크포인트 원장도 하나만 생기는지와 1/100 골드 잔여값이 DbContext 사이에서 보존되는지 검증합니다.

`GoldLedgerReaderTests`는 관리자 원장 조회가 실제 PostgreSQL에서 플레이어별 최신순으로 정렬되고 커서 이후의 과거 행만 반환하는지 검증합니다.

Docker를 직접 제어할 수 없는 CI 환경에서는 `IDLEGUILD_TEST_POSTGRES_CONNECTION_STRING` 환경 변수로 준비된 테스트 DB를 지정할 수 있습니다.

## 10. player_equipment와 equipment_change_receipts

`player_equipment`는 장비 인스턴스 ID, 플레이어 ID, 마스터 코드, 슬롯, 장착 여부, 획득 시각과 PostgreSQL `xmin`을 저장합니다. `(player_id, slot) WHERE is_equipped` 부분 유일 인덱스는 한 플레이어가 같은 슬롯에 두 장비를 동시에 장착하지 못하게 합니다.

`equipment_change_receipts`는 `(player_id, idempotency_key)`를 기본 키로 사용하고 요청 장비, 요청 상태, 결과, 교체된 장비와 처리 시각을 저장합니다. 장착 상태와 영수증은 같은 트랜잭션에서 저장됩니다.

`AddEquipmentSystem` Migration이 두 테이블, 외래 키, 체크 제약조건과 인덱스를 생성합니다. `EquipmentPersistenceTests`는 실제 PostgreSQL에서 저장 왕복, 슬롯 유일성, 장착 교체와 영수증 저장을 검증합니다.

## 11. shop_purchase_receipts

`shop_purchase_receipts`는 구매 ID, 플레이어 ID, 멱등 키, 상품 ID, 모의 가격, 지급 골드, 지급 후 잔액과 구매 시각을 저장합니다. `(player_id, idempotency_key)` 유일 인덱스가 같은 플레이어의 구매 요청을 한 번만 허용하고, 플레이어·시각 복합 인덱스가 최신순 이력을 지원합니다.

`AddMockShopPurchases` Migration이 테이블과 제약조건을 생성합니다. 골드 상태, 구매 영수증과 `ShopPurchase` 골드 원장은 한 트랜잭션에서 저장됩니다.
