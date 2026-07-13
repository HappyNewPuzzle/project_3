using System;
using System.Collections;
using UnityEngine;

public sealed class IdleGuildMockApiClient : IIdleGuildApiClient
{
    private const string TraceId = "mock-trace";
    private const string BronzeSwordEquipmentId = "mock-bronze-sword-001";
    private const string SmallGoldPackProductId = "small-gold-pack";

    private string playerId;
    private string accessToken;
    private long gold = 20;
    private int heroLevel = 1;
    private int highestStage = 1;
    private bool isBronzeSwordEquipped;

    public IEnumerator GetSystemStatus(string apiBaseUrl, Action<IdleGuildApiResult<SystemStatusResponse>> onComplete)
    {
        yield return Complete(onComplete, new SystemStatusResponse
        {
            status = "mock-ok",
            serverTimeUtc = DateTime.UtcNow.ToString("O")
        });
    }

    public IEnumerator GuestLogin(string apiBaseUrl, Action<IdleGuildApiResult<GuestLoginResponse>> onComplete)
    {
        if (string.IsNullOrWhiteSpace(playerId))
        {
            playerId = "mock-player-" + UnityEngine.Random.Range(1000, 9999);
            accessToken = "mock-access-token";
        }

        yield return Complete(onComplete, new GuestLoginResponse
        {
            playerId = playerId,
            accessToken = accessToken
        });
    }

    public IEnumerator GetGameState(string apiBaseUrl, Action<IdleGuildApiResult<GameStateResponse>> onComplete)
    {
        yield return Complete(onComplete, CreateGameState());
    }

    public IEnumerator ClaimIdleReward(string apiBaseUrl, string idempotencyKey, Action<IdleGuildApiResult<ClaimIdleRewardResponse>> onComplete)
    {
        long awarded = 100 + (highestStage - 1) * 10;
        gold += awarded;

        yield return Complete(onComplete, new ClaimIdleRewardResponse
        {
            goldAwarded = awarded,
            goldBalanceAfter = gold,
            isReplay = false
        });
    }

    public IEnumerator UpgradeMainHero(string apiBaseUrl, string idempotencyKey, Action<IdleGuildApiResult<UpgradeHeroResponse>> onComplete)
    {
        long cost = heroLevel * 30;
        string outcome = "insufficientGold";

        if (gold >= cost)
        {
            gold -= cost;
            heroLevel++;
            outcome = "succeeded";
        }

        yield return Complete(onComplete, new UpgradeHeroResponse
        {
            outcome = outcome,
            heroLevelAfter = heroLevel,
            goldCost = cost,
            goldBalanceAfter = gold,
            isReplay = false
        });
    }

    public IEnumerator ChallengeStage(string apiBaseUrl, int stage, string idempotencyKey, Action<IdleGuildApiResult<ChallengeStageResponse>> onComplete)
    {
        int targetStage = Mathf.Max(1, stage);
        int requiredPower = 10 + (targetStage - 1) * 8;
        string outcome = CreateGameState().heroPower >= requiredPower ? "succeeded" : "failed";

        if (outcome == "succeeded")
        {
            highestStage = Mathf.Max(highestStage, targetStage);
        }

        yield return Complete(onComplete, new ChallengeStageResponse
        {
            outcome = outcome,
            highestStageAfter = highestStage,
            productionBonusPercentAfter = ProductionBonusPercent,
            isReplay = false
        });
    }

    public IEnumerator GetEquipment(string apiBaseUrl, Action<IdleGuildApiResult<EquipmentInventoryResponse>> onComplete)
    {
        yield return Complete(onComplete, new EquipmentInventoryResponse
        {
            equipmentPowerBonus = EquipmentPowerBonus,
            items = new[]
            {
                new EquipmentItemResponse
                {
                    equipmentId = BronzeSwordEquipmentId,
                    definitionId = "bronze-sword",
                    name = "Bronze Sword",
                    slot = "weapon",
                    powerBonus = 5,
                    isEquipped = isBronzeSwordEquipped
                }
            }
        });
    }

    public IEnumerator Equip(string apiBaseUrl, string equipmentId, string idempotencyKey, Action<IdleGuildApiResult<ChangeEquipmentResponse>> onComplete)
    {
        if (equipmentId != BronzeSwordEquipmentId)
        {
            yield return CompleteFailure(onComplete, 404, "Equipment not found");
            yield break;
        }

        isBronzeSwordEquipped = true;

        yield return Complete(onComplete, new ChangeEquipmentResponse
        {
            equipmentId = equipmentId,
            isEquipped = true,
            outcome = "succeeded",
            replacedEquipmentId = null,
            isReplay = false
        });
    }

    public IEnumerator GetShopProducts(string apiBaseUrl, Action<IdleGuildApiResult<ShopCatalogResponse>> onComplete)
    {
        yield return Complete(onComplete, new ShopCatalogResponse
        {
            products = new[]
            {
                new ShopProductResponse
                {
                    productId = SmallGoldPackProductId,
                    name = "Small Gold Pack",
                    mockPrice = 1000,
                    goldAwarded = 100
                }
            }
        });
    }

    public IEnumerator Purchase(string apiBaseUrl, string productId, string idempotencyKey, Action<IdleGuildApiResult<ShopPurchaseResponse>> onComplete)
    {
        if (productId != SmallGoldPackProductId)
        {
            yield return CompleteFailure(onComplete, 404, "Product not found");
            yield break;
        }

        const long awarded = 100;
        gold += awarded;

        yield return Complete(onComplete, new ShopPurchaseResponse
        {
            purchaseId = "mock-purchase-" + Guid.NewGuid().ToString("N"),
            productId = productId,
            mockPrice = 1000,
            goldAwarded = awarded,
            goldBalanceAfter = gold,
            isReplay = false
        });
    }

    private int EquipmentPowerBonus => isBronzeSwordEquipped ? 5 : 0;

    private int ProductionBonusPercent => (highestStage - 1) * 5;

    private GameStateResponse CreateGameState()
    {
        return new GameStateResponse
        {
            gold = gold,
            heroLevel = heroLevel,
            highestStage = highestStage,
            productionBonusPercent = ProductionBonusPercent,
            heroPower = heroLevel * 10 + EquipmentPowerBonus,
            equipmentPowerBonus = EquipmentPowerBonus
        };
    }

    private static IEnumerator Complete<TResponse>(Action<IdleGuildApiResult<TResponse>> onComplete, TResponse response)
    {
        yield return null;
        onComplete?.Invoke(IdleGuildApiResult<TResponse>.Success(200, response, TraceId));
    }

    private static IEnumerator CompleteFailure<TResponse>(Action<IdleGuildApiResult<TResponse>> onComplete, long statusCode, string errorTitle)
    {
        yield return null;
        onComplete?.Invoke(IdleGuildApiResult<TResponse>.Failure(statusCode, errorTitle, TraceId));
    }
}
