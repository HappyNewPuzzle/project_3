using IdleGuild.Application.Accounts.CreateGuest;
using IdleGuild.Application.Admin.Players.GetAdminPlayer;
using IdleGuild.Application.Admin.Players.GetGoldLedgerPage;
using IdleGuild.Application.GameStates.GetGameState;
using IdleGuild.Application.GameStates.SyncProgression;
using IdleGuild.Application.Equipment.ChangeEquipment;
using IdleGuild.Application.Equipment.GetEquipment;
using IdleGuild.Application.Heroes.UpgradeMainHero;
using IdleGuild.Application.Rewards.ClaimIdleReward;
using IdleGuild.Application.Rewards.PreviewIdleReward;
using IdleGuild.Application.Profiles.UpdateSelectedHero;
using IdleGuild.Application.Stages.ChallengeStage;
using IdleGuild.Application.Shop.GetPurchaseHistory;
using IdleGuild.Application.Shop.GetShopCatalog;
using IdleGuild.Application.Shop.PurchaseShopProduct;
using Microsoft.Extensions.DependencyInjection;

namespace IdleGuild.Application;

/// <summary>Application 유스케이스를 의존성 컨테이너에 등록합니다.</summary>
public static class DependencyInjection
{
    /// <summary>요청마다 독립된 Handler 인스턴스를 사용하도록 구성합니다.</summary>
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<CreateGuestAccountHandler>();
        services.AddScoped<GetGameStateHandler>();
        services.AddScoped<SyncProgressionHandler>();
        services.AddScoped<ClaimIdleRewardHandler>();
        services.AddScoped<PreviewIdleRewardHandler>();
        services.AddScoped<UpdateSelectedHeroHandler>();
        services.AddScoped<UpgradeMainHeroHandler>();
        services.AddScoped<ChallengeStageHandler>();
        services.AddScoped<GetAdminPlayerHandler>();
        services.AddScoped<GetGoldLedgerPageHandler>();
        services.AddScoped<GetEquipmentHandler>();
        services.AddScoped<ChangeEquipmentHandler>();
        services.AddScoped<GetShopCatalogHandler>();
        services.AddScoped<PurchaseShopProductHandler>();
        services.AddScoped<GetPurchaseHistoryHandler>();

        return services;
    }
}
