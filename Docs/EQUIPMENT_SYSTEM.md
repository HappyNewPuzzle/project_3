# 장비 시스템

## 1. 이 기능을 추가한 이유

장비는 플레이어가 보유한 콘텐츠와 서버가 관리하는 게임 규칙을 분리해 학습하기 좋은 기능입니다. 클라이언트는 어떤 장비를 장착할지만 요청하고, 실제 소유 여부·슬롯 충돌·전투력은 서버가 판정합니다.

## 2. 데이터 구조

- `EquipmentCatalog`: 모든 플레이어에게 공통인 장비 마스터 데이터입니다. 코드, 이름, 슬롯, 전투력 보너스를 정의합니다.
- `PlayerEquipment`: 특정 플레이어가 실제로 보유한 장비 인스턴스입니다.
- `EquipmentChangeReceipt`: 같은 장착 요청이 재전송되어도 최초 결과를 돌려주는 멱등성 영수증입니다.

현재 마스터 데이터는 학습 범위를 작게 유지하기 위해 코드에 둡니다.

| 코드 | 이름 | 슬롯 | 전투력 |
| --- | --- | --- | ---: |
| `training-sword` | Training Sword | Weapon | +1 |
| `bronze-sword` | Bronze Sword | Weapon | +4 |

게스트 생성 시 두 장비를 지급하고 Training Sword를 장착합니다. 따라서 레벨 1 영웅의 기본 전투력 10에 장비 1이 더해져 총 전투력은 11입니다.

## 3. 서버 규칙

- 다른 플레이어의 장비는 조회하거나 변경할 수 없습니다.
- 보유하지 않은 장비는 장착할 수 없습니다.
- 같은 슬롯에는 한 장비만 장착할 수 있습니다.
- 새 장비를 장착하면 같은 슬롯의 기존 장비는 자동 해제됩니다.
- 장착 변경에는 `Idempotency-Key`가 필요합니다.
- 전투력은 `영웅 레벨 전투력 + 장착 장비 보너스`로 서버가 계산합니다.
- PostgreSQL 부분 유일 인덱스도 플레이어별·슬롯별 장착 장비를 하나로 제한합니다.

Application과 DB가 같은 규칙을 각각 검사하는 이유는 정상 요청에는 친절한 결과를 주면서, 동시 요청이나 구현 실수도 최종 저장 단계에서 차단하기 위해서입니다.

## 4. API

```http
GET /api/v1/equipment
Authorization: Bearer {accessToken}
```

```http
PUT /api/v1/equipment/{equipmentId}/equipped
Authorization: Bearer {accessToken}
Idempotency-Key: equip-bronze-001
Content-Type: application/json

{ "isEquipped": true }
```

응답 `outcome`은 `Succeeded` 또는 `AlreadyInDesiredState`입니다. 같은 키와 같은 요청을 다시 보내면 최초 응답이 재생되고, 같은 키를 다른 요청에 재사용하면 409를 반환합니다.

## 5. 전투와의 연결

스테이지 도전 시 Application 계층이 현재 장착 장비를 조회하고 보너스를 합산합니다. Domain은 보너스와 영웅 레벨로 최종 전투력을 계산합니다. 게임 상태의 `heroPower`와 `equipmentPowerBonus`도 같은 규칙을 사용하므로 화면 표시와 전투 판정이 일치합니다.

## 6. 검증 범위

- Domain: 마스터 데이터와 전투력 합산 규칙
- Application: 목록 조회, 같은 슬롯 교체, 멱등 재실행
- API: 인증된 목록 조회, 장착 후 게임 상태 반영, 타인 장비 차단
- Infrastructure: PostgreSQL 저장 왕복, 부분 유일 인덱스, 교체와 영수증의 원자적 저장

PostgreSQL 통합 테스트는 Docker Desktop 또는 `IDLEGUILD_TEST_POSTGRES_CONNECTION_STRING`으로 지정한 테스트 DB가 필요합니다.
