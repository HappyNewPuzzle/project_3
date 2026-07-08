using UnityEngine;

// 게스트 로그인 세션 정보를 Unity PlayerPrefs에 저장하고 복원하는 작은 저장소 클래스입니다.
public sealed class IdleGuildSession
{
    // PlayerPrefs에 accessToken을 저장할 때 사용하는 키입니다.
    private const string AccessTokenKey = "IdleGuild.AccessToken";
    // PlayerPrefs에 playerId를 저장할 때 사용하는 키입니다.
    private const string PlayerIdKey = "IdleGuild.PlayerId";

    // 서버 보호 API 호출에 사용할 JWT access token입니다.
    public string AccessToken { get; private set; }
    // 서버가 발급한 현재 게스트 플레이어 식별자입니다.
    public string PlayerId { get; private set; }
    // 토큰이 있으면 보호 API 버튼을 활성화할 수 있습니다.
    public bool HasToken => !string.IsNullOrWhiteSpace(AccessToken);

    // PlayerPrefs에서 이전 세션 정보를 불러옵니다.
    public void Load()
    {
        // 저장된 값이 없으면 빈 문자열을 사용해 null 처리를 단순화합니다.
        AccessToken = PlayerPrefs.GetString(AccessTokenKey, string.Empty);
        PlayerId = PlayerPrefs.GetString(PlayerIdKey, string.Empty);
    }

    // 게스트 로그인 응답을 세션에 저장합니다.
    public void Save(GuestLoginResponse response)
    {
        // 응답 DTO에서 accessToken과 playerId를 현재 메모리 상태에 반영합니다.
        AccessToken = response.accessToken;
        PlayerId = response.playerId;
        // Play를 다시 눌러도 세션이 유지되도록 PlayerPrefs에도 저장합니다.
        PlayerPrefs.SetString(AccessTokenKey, AccessToken);
        PlayerPrefs.SetString(PlayerIdKey, PlayerId);
        PlayerPrefs.Save();
    }

    // 저장된 세션을 삭제해 새 게스트 로그인부터 다시 시작할 수 있게 합니다.
    public void Clear()
    {
        // 메모리 상태를 먼저 비워 UI가 즉시 토큰 없음으로 갱신되게 합니다.
        AccessToken = string.Empty;
        PlayerId = string.Empty;
        // PlayerPrefs에 남아 있던 세션 값도 삭제합니다.
        PlayerPrefs.DeleteKey(AccessTokenKey);
        PlayerPrefs.DeleteKey(PlayerIdKey);
        PlayerPrefs.Save();
    }
}
