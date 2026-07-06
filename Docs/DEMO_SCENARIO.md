# 데모 시나리오

이 문서는 서버를 실행한 뒤 핵심 게임 루프를 직접 확인하는 순서를 설명합니다.

목표는 다음 흐름을 눈으로 확인하는 것입니다.

```text
게스트 생성
→ 게임 상태 조회
→ 방치 보상 수령
→ 영웅 강화
→ 스테이지 도전
→ 생산 보너스 반영 확인
```

## 1. 사전 준비

README의 로컬 실행 절차를 먼저 완료합니다.

```powershell
dotnet restore
dotnet tool restore

Copy-Item .env.example .env
docker compose up -d --wait

$env:ConnectionStrings__GameDatabase = "Host=localhost;Port=5432;Database=idleguild;Username=idleguild;Password=replace_with_local_password"
$env:Jwt__SigningKey = "replace_with_a_random_secret_of_at_least_32_bytes"
dotnet tool run dotnet-ef database update --project src/IdleGuild.Infrastructure --startup-project src/IdleGuild.Infrastructure

dotnet run --project src/IdleGuild.Api
```

서버 주소는 기본적으로 `http://localhost:5219`입니다.

## 2. 시스템 상태 확인

```powershell
Invoke-RestMethod `
  -Method Get `
  -Uri "http://localhost:5219/api/v1/system/status"
```

예상 결과:

```json
{
  "status": "ok",
  "serverTimeUtc": "..."
}
```

이 API는 클라이언트 시간이 아니라 서버 UTC 시각을 기준으로 게임을 운영한다는 점을 확인하기 위한 출발점입니다.

## 3. 게스트 계정 생성

```powershell
$guest = Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5219/api/v1/accounts/guest"

$guest
$token = $guest.accessToken
```

확인할 점:

- `playerId`가 생성됩니다.
- `accessToken`이 발급됩니다.
- 이후 보호 API는 이 토큰으로 호출합니다.

## 4. 초기 게임 상태 조회

```powershell
Invoke-RestMethod `
  -Method Get `
  -Uri "http://localhost:5219/api/v1/game-state" `
  -Headers @{ Authorization = "Bearer $token" }
```

예상 상태:

```json
{
  "gold": 0,
  "heroLevel": 1,
  "highestStage": 1,
  "productionBonusPercent": 0
}
```

신규 플레이어는 골드 0, 영웅 레벨 1, 최고 스테이지 1에서 시작합니다.

## 5. 방치 보상 수령

서버 실행 후 10초 정도 기다린 뒤 호출하면 확인하기 쉽습니다.

```powershell
$claim = Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5219/api/v1/rewards/idle/claim" `
  -Headers @{
    Authorization = "Bearer $token"
    "Idempotency-Key" = "demo-claim-001"
  }

$claim
```

확인할 점:

- `goldAwarded`가 경과 시간에 따라 지급됩니다.
- `goldBalanceAfter`가 증가합니다.
- 같은 `Idempotency-Key`로 다시 호출하면 `isReplay`가 `true`가 되고 최초 결과가 재생됩니다.

같은 키 재시도:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5219/api/v1/rewards/idle/claim" `
  -Headers @{
    Authorization = "Bearer $token"
    "Idempotency-Key" = "demo-claim-001"
  }
```

## 6. 영웅 강화

영웅 1레벨에서 2레벨로 올리는 비용은 10골드입니다. 골드가 10 이상이면 다음 요청이 성공합니다.

```powershell
$upgrade = Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5219/api/v1/heroes/main/upgrade" `
  -Headers @{
    Authorization = "Bearer $token"
    "Idempotency-Key" = "demo-upgrade-001"
  }

$upgrade
```

성공 시 확인할 점:

- `outcome`은 `succeeded`입니다.
- `heroLevelAfter`는 2입니다.
- `goldCost`는 10입니다.
- `goldBalanceAfter`는 강화 비용 차감 후 잔액입니다.

골드가 부족하면 `409 Conflict`와 함께 `outcome: insufficientGold`가 반환됩니다. 이 경우도 게임 규칙상 정상 판정이므로 같은 키 재시도에서 같은 결과가 재생됩니다.

## 7. 스테이지 2 도전

2레벨 영웅의 전투력은 `10 × 2 = 20`입니다. 스테이지 2 요구 전투력은 `floor(10 × 1.2) = 12`이므로 성공할 수 있습니다.

```powershell
$stage = Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5219/api/v1/stages/2/challenge" `
  -Headers @{
    Authorization = "Bearer $token"
    "Idempotency-Key" = "demo-stage-002"
  }

$stage
```

성공 시 확인할 점:

- `outcome`은 `succeeded`입니다.
- `highestStageAfter`는 2입니다.
- `productionBonusPercentAfter`는 5입니다.
- 성공 전까지 쌓인 방치 보상은 기존 배율로 먼저 정산됩니다.

## 8. 최종 상태 확인

```powershell
Invoke-RestMethod `
  -Method Get `
  -Uri "http://localhost:5219/api/v1/game-state" `
  -Headers @{ Authorization = "Bearer $token" }
```

확인할 점:

- `heroLevel`이 2 이상입니다.
- `highestStage`가 2입니다.
- `productionBonusPercent`가 5입니다.

이 상태부터 이후 방치 보상은 105% 생산 배율로 계산됩니다.

## 9. 오류 계약 확인

멱등 키 없이 상태 변경 API를 호출하면 `400 ProblemDetails`가 반환됩니다.

```powershell
curl.exe -i -X POST `
  "http://localhost:5219/api/v1/rewards/idle/claim" `
  -H "Authorization: Bearer $token" `
  -H "Accept: application/json"
```

확인할 점:

- HTTP 상태는 `400 Bad Request`입니다.
- Body의 `title`은 `Idempotency key is required.`입니다.

## 10. 이 데모가 보여 주는 것

- 서버가 플레이어 상태를 생성하고 JWT로 보호합니다.
- 서버 시간이 방치 보상의 기준이 됩니다.
- 같은 요청을 재시도해도 멱등 키로 중복 지급을 막습니다.
- 골드를 소비해 영웅을 성장시킵니다.
- 성장한 영웅으로 스테이지를 진행합니다.
- 스테이지 진행이 다시 생산 보너스로 연결됩니다.
- 오류 응답은 클라이언트가 처리하기 쉬운 계약으로 반환됩니다.
