namespace IdleGuild.Api.Contracts;

/// <summary>장비 인스턴스를 장착하거나 해제할 목표 상태입니다.</summary>
public sealed record ChangeEquipmentRequest(bool IsEquipped);
