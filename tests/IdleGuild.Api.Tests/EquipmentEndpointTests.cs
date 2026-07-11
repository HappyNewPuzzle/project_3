using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdleGuild.Api.Contracts;
using IdleGuild.Domain.Equipment;

namespace IdleGuild.Api.Tests;

/// <summary>장비 조회, 멱등 장착, 사용자 격리와 전투력 반영 HTTP 계약을 검증합니다.</summary>
public sealed class EquipmentEndpointTests(
    IdleGuildApiFactory factory) :
    IClassFixture<IdleGuildApiFactory>
{
    private readonly HttpClient _client =
        factory.CreateClient();

    // 신규 게스트는 훈련용 검을 장착하고 청동 검을 보유한 상태로 시작해야 합니다.
    [Fact]
    public async Task Inventory_NewGuest_HasStarterEquipment()
    {
        var guest = await CreateGuestAsync();

        var inventory = await GetInventoryAsync(
            guest.AccessToken);

        Assert.Equal(guest.PlayerId, inventory.PlayerId);
        Assert.Equal(2, inventory.Items.Count);
        Assert.Equal(1, inventory.EquipmentPowerBonus);
        var equipped = Assert.Single(
            inventory.Items,
            item => item.IsEquipped);
        Assert.Equal(
            EquipmentCatalog.TrainingSwordId,
            equipped.DefinitionId);
    }

    // 청동 검 장착은 기존 검을 교체하고 게임 상태의 서버 전투력을 14로 갱신해야 합니다.
    [Fact]
    public async Task EquipBronzeSword_ReplaysAndUpdatesHeroPower()
    {
        var guest = await CreateGuestAsync();
        var inventory = await GetInventoryAsync(
            guest.AccessToken);
        var bronze = inventory.Items.Single(item =>
            item.DefinitionId == EquipmentCatalog.BronzeSwordId);
        var key = $"equip-{Guid.NewGuid():N}";

        var first = await ChangeAsync(
            guest.AccessToken,
            bronze.EquipmentId,
            isEquipped: true,
            key);
        var replay = await ChangeAsync(
            guest.AccessToken,
            bronze.EquipmentId,
            isEquipped: true,
            key);
        var state = await GetGameStateAsync(
            guest.AccessToken);

        Assert.Equal("succeeded", first.Outcome);
        Assert.NotNull(first.ReplacedEquipmentId);
        Assert.False(first.IsReplay);
        Assert.True(replay.IsReplay);
        Assert.Equal(4, state.EquipmentPowerBonus);
        Assert.Equal(14, state.HeroPower);
    }

    // 다른 플레이어가 소유한 인스턴스 ID를 알아도 장착 상태를 변경할 수 없어야 합니다.
    [Fact]
    public async Task Change_OtherPlayersEquipment_ReturnsNotFound()
    {
        var owner = await CreateGuestAsync();
        var attacker = await CreateGuestAsync();
        var ownerInventory = await GetInventoryAsync(
            owner.AccessToken);
        var targetId = ownerInventory.Items[0].EquipmentId;
        using var request = CreateChangeRequest(
            attacker.AccessToken,
            targetId,
            isEquipped: true,
            $"attack-{Guid.NewGuid():N}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<GuestAccountResponse> CreateGuestAsync()
    {
        using var response = await _client.PostAsync(
            "/api/v1/accounts/guest",
            content: null);
        var guest = await response.Content
            .ReadFromJsonAsync<GuestAccountResponse>();
        return Assert.IsType<GuestAccountResponse>(guest);
    }

    private async Task<EquipmentInventoryResponse>
        GetInventoryAsync(string token)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/equipment");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        using var response = await _client.SendAsync(request);
        var inventory = await response.Content
            .ReadFromJsonAsync<EquipmentInventoryResponse>();
        return Assert.IsType<EquipmentInventoryResponse>(
            inventory);
    }

    private async Task<ChangeEquipmentResponse> ChangeAsync(
        string token,
        Guid equipmentId,
        bool isEquipped,
        string idempotencyKey)
    {
        using var request = CreateChangeRequest(
            token,
            equipmentId,
            isEquipped,
            idempotencyKey);
        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content
            .ReadFromJsonAsync<ChangeEquipmentResponse>();
        return Assert.IsType<ChangeEquipmentResponse>(result);
    }

    private async Task<GameStateResponse> GetGameStateAsync(
        string token)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/game-state");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        using var response = await _client.SendAsync(request);
        var state = await response.Content
            .ReadFromJsonAsync<GameStateResponse>();
        return Assert.IsType<GameStateResponse>(state);
    }

    private static HttpRequestMessage CreateChangeRequest(
        string token,
        Guid equipmentId,
        bool isEquipped,
        string idempotencyKey)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/v1/equipment/{equipmentId:D}/equipped")
        {
            Content = JsonContent.Create(
                new ChangeEquipmentRequest(isEquipped))
        };
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add(
            "Idempotency-Key",
            idempotencyKey);
        return request;
    }
}
