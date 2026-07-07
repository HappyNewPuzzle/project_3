using UnityEngine;

public sealed class IdleGuildSession
{
    private const string AccessTokenKey = "IdleGuild.AccessToken";
    private const string PlayerIdKey = "IdleGuild.PlayerId";

    public string AccessToken { get; private set; }
    public string PlayerId { get; private set; }
    public bool HasToken => !string.IsNullOrWhiteSpace(AccessToken);

    public void Load()
    {
        AccessToken = PlayerPrefs.GetString(AccessTokenKey, string.Empty);
        PlayerId = PlayerPrefs.GetString(PlayerIdKey, string.Empty);
    }

    public void Save(GuestLoginResponse response)
    {
        AccessToken = response.accessToken;
        PlayerId = response.playerId;
        PlayerPrefs.SetString(AccessTokenKey, AccessToken);
        PlayerPrefs.SetString(PlayerIdKey, PlayerId);
        PlayerPrefs.Save();
    }

    public void Clear()
    {
        AccessToken = string.Empty;
        PlayerId = string.Empty;
        PlayerPrefs.DeleteKey(AccessTokenKey);
        PlayerPrefs.DeleteKey(PlayerIdKey);
        PlayerPrefs.Save();
    }
}
