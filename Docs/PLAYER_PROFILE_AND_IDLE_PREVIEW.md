# 선택 영웅 저장과 방치 보상 미리보기

## 1. 작업 목적

Unity의 화면 선택을 로컬 enum 숫자로만 보관하면 enum 순서 변경, 재설치, 다른 기기 로그인 때 선택값을 안정적으로 복원할 수 없습니다. 따라서 서버는 화면 구현과 독립적인 문자열 ID를 플레이어 상태에 저장합니다.

방치 보상은 클라이언트가 시간을 계산하지 않고 서버 UTC 시각과 저장 상태로만 계산해야 합니다. 미리보기와 실제 수령이 서로 다른 공식을 사용하면 UI 표시값과 지급값이 달라지므로 두 기능 모두 Domain의 같은 `IdleRewardPolicy`를 사용합니다.

## 2. 선택 영웅 규칙

허용되는 안정 ID는 다음 세 개입니다.

- `girl` (신규 및 기존 플레이어 기본값)
- `black_cat`
- `classic`

서버는 Unity `CharacterVisualSet` enum의 정수값을 저장하지 않습니다. `SelectedHeroPolicy`가 허용값을 관리하고 `PlayerGameState.SelectHero`가 도메인 불변식을 지킵니다. PostgreSQL에도 같은 허용값 Check Constraint가 있어 잘못된 값의 직접 저장을 차단합니다.

### 선택 변경 API

```http
PUT /api/v1/profile/selected-hero
Authorization: Bearer {access-token}
Content-Type: application/json

{
  "selectedHeroId": "black_cat"
}
```

성공 응답:

```json
{
  "selectedHeroId": "black_cat"
}
```

지원하지 않는 값은 `400 Validation Problem`이며 오류 키는 `selectedHeroId`입니다. 현재 값과 같은 ID를 다시 보내도 성공합니다. 갱신은 기존 `PlayerGameState`의 PostgreSQL `xmin` 충돌 감지와 최대 3회 재조회·재시도 패턴을 사용합니다.

`GET /api/v1/game-state`에도 `selectedHeroId`가 포함됩니다.

## 3. 방치 보상 미리보기

```http
GET /api/v1/rewards/idle/preview
Authorization: Bearer {access-token}
```

성공 응답:

```json
{
  "elapsedSeconds": 3600,
  "claimableGold": 3600,
  "maximumAccumulationSeconds": 28800,
  "calculatedAtUtc": "2026-07-17T00:00:00+00:00"
}
```

미리보기는 `AsNoTracking` 조회 결과에 `PlayerGameState.PreviewIdleReward`를 실행하며 `SaveChanges`를 호출하지 않습니다. 골드, 마지막 수령 시각, remainder, 수령 영수증과 골드 원장을 바꾸지 않습니다.

경과 시간은 서버 `TimeProvider`의 UTC 시각으로 계산하고 최대 28,800초로 제한합니다. 클라이언트 시각이나 클라이언트가 계산한 골드 값은 요청으로 받지 않습니다. 같은 서버 시각에서 preview와 claim을 실행하면 `claimableGold`와 `goldAwarded`가 같습니다.

## 4. 계층별 책임

- Domain: 허용 영웅 ID와 방치 보상 계산 규칙, 상태 변경 여부 결정
- Application: 유스케이스 실행, 선택 갱신 충돌 재시도, 읽기 전용 미리보기
- Infrastructure: 문자열 열·기본값·Check Constraint·`xmin` 매핑과 PostgreSQL 저장
- API: JWT 경계, 요청 검증, camelCase DTO와 ProblemDetails 변환
- Tests: 규칙, Handler, HTTP 계약, 실제 PostgreSQL Migration과 영속성 검증

## 5. Migration과 배포

Migration은 `20260716225618_AddSelectedHero`입니다. `selected_hero_id varchar(32) NOT NULL DEFAULT 'girl'`을 추가하므로 기존 행도 적용 시 `girl`로 채워집니다. Migration을 적용한 뒤 새 API 인스턴스를 배포합니다.

## 6. Unity 연동 항목

Unity DTO에는 다음 필드가 필요합니다.

```csharp
public string selectedHeroId;
```

추가할 계약은 다음과 같습니다.

- `GameStateResponse.selectedHeroId`
- `UpdateSelectedHeroRequest.selectedHeroId`
- `UpdateSelectedHeroResponse.selectedHeroId`
- `IdleRewardPreviewResponse.elapsedSeconds`
- `IdleRewardPreviewResponse.claimableGold`
- `IdleRewardPreviewResponse.maximumAccumulationSeconds`
- `IdleRewardPreviewResponse.calculatedAtUtc`

클라이언트는 enum과 문자열을 명시적으로 매핑하고 enum의 정수 캐스팅을 네트워크에 사용하지 않습니다. 로그인 후 game-state의 값을 화면에 복원하고, 영웅 선택 시 `PUT /api/v1/profile/selected-hero`, 방치 보상 팝업을 열 때 `GET /api/v1/rewards/idle/preview`를 호출합니다.

## 7. 다음 서버 우선순위

1. 장비 판매·분해·합성 API
2. 출석·우편·임무 서버 저장
3. 보석과 상점 구매 내역 서버 권위화
