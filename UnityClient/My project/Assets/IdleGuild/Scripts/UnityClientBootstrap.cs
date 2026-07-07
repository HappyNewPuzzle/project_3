using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public sealed class UnityClientBootstrap : MonoBehaviour
{
    [SerializeField] private string apiBaseUrl = "http://localhost:5219";

    private readonly StringBuilder log = new StringBuilder();
    private string accessToken;
    private string playerId;
    private string stageInput = "2";
    private bool isBusy;
    private bool isDemoRunning;
    private bool lastRequestSucceeded;
    private GameStateResponse gameState;

    private void Awake()
    {
        accessToken = PlayerPrefs.GetString("IdleGuild.AccessToken", string.Empty);
        playerId = PlayerPrefs.GetString("IdleGuild.PlayerId", string.Empty);
        AddLog("Ready. Server: " + apiBaseUrl);
    }

    private void OnGUI()
    {
        const int padding = 18;
        const int width = 520;
        GUILayout.BeginArea(new Rect(padding, padding, width, Screen.height - padding * 2), GUI.skin.box);
        GUILayout.Label("Idle Guild Unity Client");
        GUILayout.Label("Server: " + apiBaseUrl);
        GUILayout.Label("Player: " + (string.IsNullOrEmpty(playerId) ? "(not logged in)" : playerId));
        GUILayout.Label("Token: " + (string.IsNullOrEmpty(accessToken) ? "(none)" : "saved"));

        GUILayout.Space(8);
        GUI.enabled = !isBusy && !isDemoRunning;
        if (GUILayout.Button("Check Server Status"))
        {
            StartCoroutine(GetSystemStatus());
        }

        if (GUILayout.Button("Run Demo Flow"))
        {
            StartCoroutine(RunDemoFlow());
        }

        if (GUILayout.Button("1. Guest Login"))
        {
            StartCoroutine(GuestLoginAndLoadState());
        }

        GUI.enabled = !isBusy && !isDemoRunning && HasToken;
        if (GUILayout.Button("2. Get Game State"))
        {
            StartCoroutine(GetGameState());
        }

        if (GUILayout.Button("3. Claim Idle Reward"))
        {
            StartCoroutine(PostWithIdempotency<ClaimIdleRewardResponse>(
                "/api/v1/rewards/idle/claim",
                "idle-claim",
                response =>
                {
                    AddLog("Idle reward: +" + response.goldAwarded + " gold, balance " + response.goldBalanceAfter + ReplayText(response.isReplay));
                    StartCoroutine(GetGameState());
                }));
        }

        if (GUILayout.Button("4. Upgrade Main Hero"))
        {
            StartCoroutine(PostWithIdempotency<UpgradeHeroResponse>(
                "/api/v1/heroes/main/upgrade",
                "hero-upgrade",
                response =>
                {
                    AddLog("Hero upgrade: " + response.outcome + ", level " + response.heroLevelAfter + ", cost " + response.goldCost + ReplayText(response.isReplay));
                    StartCoroutine(GetGameState());
                }));
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Stage", GUILayout.Width(48));
        stageInput = GUILayout.TextField(stageInput, GUILayout.Width(80));
        GUILayout.EndHorizontal();

        if (GUILayout.Button("5. Challenge Stage"))
        {
            int stage = ParseStageInput();
            StartCoroutine(PostWithIdempotency<ChallengeStageResponse>(
                "/api/v1/stages/" + stage + "/challenge",
                "stage-" + stage,
                response =>
                {
                    AddLog("Stage " + stage + ": " + response.outcome + ", highest " + response.highestStageAfter + ", bonus " + response.productionBonusPercentAfter + "%" + ReplayText(response.isReplay));
                    StartCoroutine(GetGameState());
                }));
        }

        GUI.enabled = !isDemoRunning;
        if (GUILayout.Button("Clear Saved Session"))
        {
            ClearSession();
        }

        GUI.enabled = true;
        GUILayout.Space(8);
        DrawGameState();
        GUILayout.Space(8);
        GUILayout.TextArea(log.ToString(), GUILayout.ExpandHeight(true));
        GUILayout.EndArea();
    }

    private bool HasToken => !string.IsNullOrWhiteSpace(accessToken);

    private IEnumerator GetSystemStatus()
    {
        yield return Send<SystemStatusResponse>(
            UnityWebRequest.kHttpVerbGET,
            "/api/v1/system/status",
            null,
            false,
            response => AddLog("Server status: " + response.status + " at " + response.serverTimeUtc));
    }

    private IEnumerator RunDemoFlow()
    {
        isDemoRunning = true;
        AddLog("Demo flow started.");
        yield return GetSystemStatus();
        if (!ShouldContinueDemo())
        {
            yield break;
        }

        yield return GuestLogin();
        if (!ShouldContinueDemo())
        {
            yield break;
        }

        yield return GetGameState();
        if (!ShouldContinueDemo())
        {
            yield break;
        }

        AddLog("Waiting 10 seconds for idle reward...");
        yield return new WaitForSeconds(10f);

        yield return PostWithIdempotency<ClaimIdleRewardResponse>(
            "/api/v1/rewards/idle/claim",
            "demo-idle-claim",
            response => AddLog("Demo claim: +" + response.goldAwarded + " gold, balance " + response.goldBalanceAfter + ReplayText(response.isReplay)));
        if (!ShouldContinueDemo())
        {
            yield break;
        }

        yield return PostWithIdempotency<UpgradeHeroResponse>(
            "/api/v1/heroes/main/upgrade",
            "demo-hero-upgrade",
            response => AddLog("Demo upgrade: " + response.outcome + ", level " + response.heroLevelAfter + ", cost " + response.goldCost + ReplayText(response.isReplay)));
        if (!ShouldContinueDemo())
        {
            yield break;
        }

        int stage = ParseStageInput();
        yield return PostWithIdempotency<ChallengeStageResponse>(
            "/api/v1/stages/" + stage + "/challenge",
            "demo-stage-" + stage,
            response => AddLog("Demo stage " + stage + ": " + response.outcome + ", highest " + response.highestStageAfter + ", bonus " + response.productionBonusPercentAfter + "%" + ReplayText(response.isReplay)));
        if (!ShouldContinueDemo())
        {
            yield break;
        }

        yield return GetGameState();
        AddLog("Demo flow finished.");
        isDemoRunning = false;
    }

    private IEnumerator GuestLogin()
    {
        yield return Send<GuestLoginResponse>(
            UnityWebRequest.kHttpVerbPOST,
            "/api/v1/accounts/guest",
            null,
            false,
            response =>
            {
                accessToken = response.accessToken;
                playerId = response.playerId;
                PlayerPrefs.SetString("IdleGuild.AccessToken", accessToken);
                PlayerPrefs.SetString("IdleGuild.PlayerId", playerId);
                PlayerPrefs.Save();
                AddLog("Guest login succeeded. Player: " + playerId);
            });
    }

    private IEnumerator GuestLoginAndLoadState()
    {
        yield return GuestLogin();
        if (lastRequestSucceeded)
        {
            yield return GetGameState();
        }
    }

    private IEnumerator GetGameState()
    {
        yield return Send<GameStateResponse>(
            UnityWebRequest.kHttpVerbGET,
            "/api/v1/game-state",
            null,
            true,
            response =>
            {
                gameState = response;
                AddLog("State: gold " + response.gold + ", hero " + response.heroLevel + ", stage " + response.highestStage + ", bonus " + response.productionBonusPercent + "%");
            });
    }

    private IEnumerator PostWithIdempotency<TResponse>(string path, string keyPrefix, Action<TResponse> onSuccess)
    {
        string idempotencyKey = keyPrefix + "-" + Guid.NewGuid().ToString("N");
        yield return Send(UnityWebRequest.kHttpVerbPOST, path, idempotencyKey, true, onSuccess);
    }

    private IEnumerator Send<TResponse>(string method, string path, string idempotencyKey, bool includeAuthorization, Action<TResponse> onSuccess)
    {
        isBusy = true;
        lastRequestSucceeded = false;
        string url = apiBaseUrl.TrimEnd('/') + path;

        using (UnityWebRequest request = new UnityWebRequest(url, method))
        {
            request.downloadHandler = new DownloadHandlerBuffer();

            if (method == UnityWebRequest.kHttpVerbPOST)
            {
                request.uploadHandler = new UploadHandlerRaw(new byte[0]);
                request.SetRequestHeader("Content-Type", "application/json");
            }

            request.SetRequestHeader("Accept", "application/json");
            if (includeAuthorization && HasToken)
            {
                request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            }

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                request.SetRequestHeader("Idempotency-Key", idempotencyKey);
            }

            yield return request.SendWebRequest();
            isBusy = false;

            string body = request.downloadHandler.text;
            if (request.result != UnityWebRequest.Result.Success)
            {
                AddError(request.responseCode, body);
                yield break;
            }

            TResponse response = JsonUtility.FromJson<TResponse>(body);
            lastRequestSucceeded = true;
            onSuccess?.Invoke(response);
        }
    }

    private void DrawGameState()
    {
        if (gameState == null)
        {
            GUILayout.Label("Game State: (not loaded)");
            return;
        }

        GUILayout.Label("Gold: " + gameState.gold);
        GUILayout.Label("Hero Level: " + gameState.heroLevel);
        GUILayout.Label("Highest Stage: " + gameState.highestStage);
        GUILayout.Label("Production Bonus: " + gameState.productionBonusPercent + "%");
    }

    private void AddError(long statusCode, string body)
    {
        string title = "Request failed without response body.";
        if (!string.IsNullOrWhiteSpace(body))
        {
            ProblemDetails problem = JsonUtility.FromJson<ProblemDetails>(body);
            title = problem == null || string.IsNullOrWhiteSpace(problem.title) ? body : problem.title;
        }

        AddLog("HTTP " + statusCode + ": " + title);
    }

    private int ParseStageInput()
    {
        int stage;
        if (!int.TryParse(stageInput, out stage) || stage < 1)
        {
            stage = 1;
            stageInput = "1";
        }

        return stage;
    }

    private void ClearSession()
    {
        accessToken = string.Empty;
        playerId = string.Empty;
        gameState = null;
        PlayerPrefs.DeleteKey("IdleGuild.AccessToken");
        PlayerPrefs.DeleteKey("IdleGuild.PlayerId");
        PlayerPrefs.Save();
        AddLog("Saved session cleared.");
    }

    private bool ShouldContinueDemo()
    {
        if (lastRequestSucceeded)
        {
            return true;
        }

        AddLog("Demo flow stopped.");
        isDemoRunning = false;
        return false;
    }

    private void AddLog(string message)
    {
        log.Insert(0, "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + Environment.NewLine);
    }

    private static string ReplayText(bool isReplay)
    {
        return isReplay ? " (replay)" : string.Empty;
    }

    [Serializable]
    private sealed class SystemStatusResponse
    {
        public string status;
        public string serverTimeUtc;
    }

    [Serializable]
    private sealed class GuestLoginResponse
    {
        public string playerId;
        public string accessToken;
    }

    [Serializable]
    private sealed class GameStateResponse
    {
        public long gold;
        public int heroLevel;
        public int highestStage;
        public int productionBonusPercent;
    }

    [Serializable]
    private sealed class ClaimIdleRewardResponse
    {
        public long goldAwarded;
        public long goldBalanceAfter;
        public bool isReplay;
    }

    [Serializable]
    private sealed class UpgradeHeroResponse
    {
        public string outcome;
        public int heroLevelAfter;
        public long goldCost;
        public long goldBalanceAfter;
        public bool isReplay;
    }

    [Serializable]
    private sealed class ChallengeStageResponse
    {
        public string outcome;
        public int highestStageAfter;
        public int productionBonusPercentAfter;
        public bool isReplay;
    }

    [Serializable]
    private sealed class ProblemDetails
    {
        public string title;
    }
}
