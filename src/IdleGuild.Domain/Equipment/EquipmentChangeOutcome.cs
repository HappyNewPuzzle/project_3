namespace IdleGuild.Domain.Equipment;

/// <summary>장착 상태 변경이 실제 반영됐는지 이미 같은 상태였는지 구분합니다.</summary>
public enum EquipmentChangeOutcome
{
    Succeeded = 1,
    AlreadyInDesiredState = 2
}
