using System;

// API 호출의 성공/실패와 응답 DTO를 하나로 담는 공통 결과 타입입니다.
[Serializable]
public sealed class IdleGuildApiResult<TResponse>
{
    // HTTP 요청과 DTO 파싱이 성공했는지 나타냅니다.
    public bool succeeded;
    // 서버가 반환한 HTTP 상태 코드입니다.
    public long statusCode;
    // 실패 시 화면 로그에 표시할 오류 제목입니다.
    public string errorTitle;
    // 서버 로그와 클라이언트 오류를 연결하는 응답 Trace ID입니다.
    public string traceId;
    // 성공 시 서버 응답 JSON이 매핑된 DTO입니다.
    public TResponse response;

    // 성공 결과 객체를 생성합니다.
    public static IdleGuildApiResult<TResponse> Success(long statusCode, TResponse response, string traceId)
    {
        // 호출자가 성공 여부, 상태 코드, 응답 DTO를 한 번에 받을 수 있게 구성합니다.
        return new IdleGuildApiResult<TResponse>
        {
            succeeded = true,
            statusCode = statusCode,
            response = response,
            traceId = traceId
        };
    }

    // 실패 결과 객체를 생성합니다.
    public static IdleGuildApiResult<TResponse> Failure(long statusCode, string errorTitle, string traceId)
    {
        // 실패 시에는 response 없이 상태 코드와 오류 제목만 채웁니다.
        return new IdleGuildApiResult<TResponse>
        {
            succeeded = false,
            statusCode = statusCode,
            errorTitle = errorTitle,
            traceId = traceId
        };
    }
}

// GET /api/v1/system/status 응답 DTO입니다.
[Serializable]
public sealed class SystemStatusResponse
{
    // 서버 상태 문자열이며 정상일 때 보통 ok입니다.
    public string status;
    // 서버 기준 UTC 시간 문자열입니다.
    public string serverTimeUtc;
}

// POST /api/v1/accounts/guest 응답 DTO입니다.
[Serializable]
public sealed class GuestLoginResponse
{
    // 새로 생성되거나 식별된 게스트 플레이어 ID입니다.
    public string playerId;
    // 이후 보호 API 호출에 사용할 JWT access token입니다.
    public string accessToken;
}

// GET /api/v1/game-state 응답 DTO입니다.
[Serializable]
public sealed class GameStateResponse
{
    // 현재 보유 골드입니다.
    public long gold;
    // 메인 영웅 레벨입니다.
    public int heroLevel;
    // 플레이어가 도달한 최고 스테이지입니다.
    public int highestStage;
    public int attackLevel;
    public int attackSpeedLevel;
    public int criticalLevel;
    public int prestigeLevel;
    public int soulStones;
    public int equipmentTier;
    public int equipmentCount;
    public int unlockedRegion;
    public int skillOneLevel;
    public int skillTwoLevel;
    public int skillThreeLevel;
    // 스테이지 진행으로 얻은 생산 보너스 퍼센트입니다.
    public int productionBonusPercent;
    // 영웅 레벨과 장착 장비를 합산한 서버 권위 전투력입니다.
    public int heroPower;
    // 현재 장착 장비가 제공하는 전투력 합계입니다.
    public int equipmentPowerBonus;
}

[Serializable]
public sealed class SyncProgressionRequest
{
    public int attackLevel;
    public int attackSpeedLevel;
    public int criticalLevel;
    public int prestigeLevel;
    public int soulStones;
    public int equipmentTier;
    public int equipmentCount;
    public int unlockedRegion;
    public int skillOneLevel;
    public int skillTwoLevel;
    public int skillThreeLevel;
}

[Serializable]
public sealed class SyncProgressionResponse
{
    public int attackLevel;
    public int attackSpeedLevel;
    public int criticalLevel;
    public int prestigeLevel;
    public int soulStones;
    public int equipmentTier;
    public int equipmentCount;
    public int unlockedRegion;
    public int skillOneLevel;
    public int skillTwoLevel;
    public int skillThreeLevel;
}

// GET /api/v1/equipment의 장비 한 개입니다.
[Serializable]
public sealed class EquipmentItemResponse
{
    public string equipmentId;
    public string definitionId;
    public string name;
    public string slot;
    public int powerBonus;
    public bool isEquipped;
}

// 보유 장비 목록과 장착 보너스 응답입니다.
[Serializable]
public sealed class EquipmentInventoryResponse
{
    public int equipmentPowerBonus;
    public EquipmentItemResponse[] items;
}

// 장비 장착·해제 응답입니다.
[Serializable]
public sealed class ChangeEquipmentResponse
{
    public string equipmentId;
    public bool isEquipped;
    public string outcome;
    public string replacedEquipmentId;
    public bool isReplay;
}

// 상점 카탈로그의 상품 한 개입니다.
[Serializable]
public sealed class ShopProductResponse
{
    public string productId;
    public string name;
    public int mockPrice;
    public long goldAwarded;
}

// GET /api/v1/shop/products 응답입니다.
[Serializable]
public sealed class ShopCatalogResponse
{
    public ShopProductResponse[] products;
}

// 모의 구매 승인과 골드 지급 응답입니다.
[Serializable]
public sealed class ShopPurchaseResponse
{
    public string purchaseId;
    public string productId;
    public int mockPrice;
    public long goldAwarded;
    public long goldBalanceAfter;
    public bool isReplay;
}

// POST /api/v1/rewards/idle/claim 응답 DTO입니다.
[Serializable]
public sealed class ClaimIdleRewardResponse
{
    // 이번 수령으로 지급된 골드량입니다.
    public long goldAwarded;
    // 지급 이후 서버 기준 골드 잔액입니다.
    public long goldBalanceAfter;
    // 같은 Idempotency-Key 재요청으로 이전 결과가 재생됐는지 여부입니다.
    public bool isReplay;
}

// POST /api/v1/heroes/main/upgrade 응답 DTO입니다.
[Serializable]
public sealed class UpgradeHeroResponse
{
    // 강화 결과 문자열이며 succeeded 또는 insufficientGold 같은 값이 올 수 있습니다.
    public string outcome;
    // 강화 처리 후 영웅 레벨입니다.
    public int heroLevelAfter;
    // 이번 강화에 사용된 골드 비용입니다.
    public long goldCost;
    // 강화 처리 후 골드 잔액입니다.
    public long goldBalanceAfter;
    // 같은 Idempotency-Key 재요청으로 이전 결과가 재생됐는지 여부입니다.
    public bool isReplay;
}

// POST /api/v1/stages/{stage}/challenge 응답 DTO입니다.
[Serializable]
public sealed class ChallengeStageResponse
{
    // 스테이지 도전 결과 문자열입니다.
    public string outcome;
    // 도전 처리 후 최고 스테이지입니다.
    public int highestStageAfter;
    // 도전 처리 후 생산 보너스 퍼센트입니다.
    public int productionBonusPercentAfter;
    // 같은 Idempotency-Key 재요청으로 이전 결과가 재생됐는지 여부입니다.
    public bool isReplay;
}

// 서버 오류 응답인 ProblemDetails에서 현재 클라이언트가 사용하는 최소 필드입니다.
[Serializable]
public sealed class ProblemDetails
{
    // 사용자/개발자에게 보여줄 오류 제목입니다.
    public string title;
    // 오류 Body에도 포함되는 서버 Trace ID입니다.
    public string traceId;
}
