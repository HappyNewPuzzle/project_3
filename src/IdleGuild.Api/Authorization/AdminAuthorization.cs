namespace IdleGuild.Api.Authorization;

/// <summary>일반 게스트 토큰과 관리자 JWT를 구분하는 권한 정책 이름과 Claim을 정의합니다.</summary>
public static class AdminAuthorization
{
    public const string PolicyName = "admin-only";
    public const string AccountTypeClaim = "account_type";
    public const string AdminAccountType = "admin";
}
