using System;
using System.Collections;
using System.Text;
using UnityEngine;

public sealed class UnityClientBootstrap : MonoBehaviour
{
    [SerializeField] private string apiBaseUrl = "http://localhost:5219";

    private readonly IdleGuildSession session = new IdleGuildSession();
    private readonly StringBuilder log = new StringBuilder();

    private IdleGuildApiClient api;
    private string stageInput = "2";
    private bool isBusy;
    private bool isDemoRunning;
    private bool lastRequestSucceeded;
    private GameStateResponse gameState;

    private void Awake()
    {
        session.Load();
        api = new IdleGuildApiClient(() => session.AccessToken);
        AddLog("Ready. Server: " + apiBaseUrl);
    }

    private void OnGUI()
    {
        const int padding = 18;
        const int width = 520;
        GUILayout.BeginArea(new Rect(padding, padding, width, Screen.height - padding * 2), GUI.skin.box);
        GUILayout.Label("Idle Guild Unity Client");
        GUILayout.Label("Server: " + apiBaseUrl);
        GUILayout.Label("Player: " + (string.IsNullOrEmpty(session.PlayerId) ? "(not logged in)" : session.PlayerId));
        GUILayout.Label("Token: " + (session.HasToken ? "saved" : "(none)"));

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

        GUI.enabled = !isBusy && !isDemoRunning && session.HasToken;
        if (GUILayout.Button("2. Get Game State"))
        {
            StartCoroutine(GetGameState());
        }

        if (GUILayout.Button("3. Claim Idle Reward"))
        {
            StartCoroutine(ClaimIdleReward("idle-claim"));
        }

        if (GUILayout.Button("4. Upgrade Main Hero"))
        {
            StartCoroutine(UpgradeMainHero("hero-upgrade"));
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Stage", GUILayout.Width(48));
        stageInput = GUILayout.TextField(stageInput, GUILayout.Width(80));
        GUILayout.EndHorizontal();

        if (GUILayout.Button("5. Challenge Stage"))
        {
            StartCoroutine(ChallengeStage(ParseStageInput(), "stage"));
        }

        GUI.enabled = !isBusy && !isDemoRunning;
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

    private IEnumerator GetSystemStatus()
    {
        yield return RunRequest(
            api.GetSystemStatus(apiBaseUrl, result =>
            {
                if (HandleFailure(result))
                {
                    return;
                }

                AddLog("Server status: " + result.response.status + " at " + result.response.serverTimeUtc);
            }));
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

        yield return ClaimIdleReward("demo-idle-claim");
        if (!ShouldContinueDemo())
        {
            yield break;
        }

        yield return UpgradeMainHero("demo-hero-upgrade");
        if (!ShouldContinueDemo())
        {
            yield break;
        }

        int stage = ParseStageInput();
        yield return ChallengeStage(stage, "demo-stage");
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
        yield return RunRequest(
            api.GuestLogin(apiBaseUrl, result =>
            {
                if (HandleFailure(result))
                {
                    return;
                }

                session.Save(result.response);
                AddLog("Guest login succeeded. Player: " + session.PlayerId);
            }));
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
        yield return RunRequest(
            api.GetGameState(apiBaseUrl, result =>
            {
                if (HandleFailure(result))
                {
                    return;
                }

                gameState = result.response;
                AddLog("State: gold " + gameState.gold + ", hero " + gameState.heroLevel + ", stage " + gameState.highestStage + ", bonus " + gameState.productionBonusPercent + "%");
            }));
    }

    private IEnumerator ClaimIdleReward(string keyPrefix)
    {
        yield return RunRequest(
            api.ClaimIdleReward(apiBaseUrl, CreateIdempotencyKey(keyPrefix), result =>
            {
                if (HandleFailure(result))
                {
                    return;
                }

                AddLog("Idle reward: +" + result.response.goldAwarded + " gold, balance " + result.response.goldBalanceAfter + ReplayText(result.response.isReplay));
            }));

        if (!isDemoRunning && lastRequestSucceeded)
        {
            yield return GetGameState();
        }
    }

    private IEnumerator UpgradeMainHero(string keyPrefix)
    {
        yield return RunRequest(
            api.UpgradeMainHero(apiBaseUrl, CreateIdempotencyKey(keyPrefix), result =>
            {
                if (HandleFailure(result))
                {
                    return;
                }

                AddLog("Hero upgrade: " + result.response.outcome + ", level " + result.response.heroLevelAfter + ", cost " + result.response.goldCost + ReplayText(result.response.isReplay));
            }));

        if (!isDemoRunning && lastRequestSucceeded)
        {
            yield return GetGameState();
        }
    }

    private IEnumerator ChallengeStage(int stage, string keyPrefix)
    {
        yield return RunRequest(
            api.ChallengeStage(apiBaseUrl, stage, CreateIdempotencyKey(keyPrefix + "-" + stage), result =>
            {
                if (HandleFailure(result))
                {
                    return;
                }

                AddLog("Stage " + stage + ": " + result.response.outcome + ", highest " + result.response.highestStageAfter + ", bonus " + result.response.productionBonusPercentAfter + "%" + ReplayText(result.response.isReplay));
            }));

        if (!isDemoRunning && lastRequestSucceeded)
        {
            yield return GetGameState();
        }
    }

    private IEnumerator RunRequest(IEnumerator request)
    {
        isBusy = true;
        lastRequestSucceeded = false;
        yield return request;
        isBusy = false;
    }

    private bool HandleFailure<TResponse>(IdleGuildApiResult<TResponse> result)
    {
        lastRequestSucceeded = result.succeeded;
        if (result.succeeded)
        {
            return false;
        }

        AddLog("HTTP " + result.statusCode + ": " + result.errorTitle);
        return true;
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
        session.Clear();
        gameState = null;
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

    private static string CreateIdempotencyKey(string keyPrefix)
    {
        return keyPrefix + "-" + Guid.NewGuid().ToString("N");
    }
}
