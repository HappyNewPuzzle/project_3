namespace IdleGuild.Application.Abstractions.Authentication;

/// <summary>Application이 구체적인 JWT 기술을 모르고 토큰을 발급하게 합니다.</summary>
public interface IAccessTokenIssuer
{
    AccessToken Issue(Guid playerId);
}
