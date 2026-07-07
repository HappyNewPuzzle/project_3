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
    private IdleGuildRuntimeUi ui;
    private string stageInput = "2";
    private bool isBusy;
    private bool isDemoRunning;
    private bool lastRequestSucceeded;
    private GameStateResponse gameState;

    private void Awake()
    {
        session.Load();
        api = new IdleGuildApiClient(() => session.AccessToken);
        ui = new IdleGuildRuntimeUi();
        ui.Build(
            transform,
            stageInput,
            () => StartCoroutine(GetSystemStatus()),
            () => StartCoroutine(RunDemoFlow()),
            () => StartCoroutine(GuestLoginAndLoadState()),
            () => StartCoroutine(GetGameState()),
            () => StartCoroutine(ClaimIdleReward("idle-claim")),
            () => StartCoroutine(UpgradeMainHero("hero-upgrade")),
            stage => StartCoroutine(ChallengeStage(stage, "stage")),
            ClearSession);
        AddLog("Ready. Server: " + apiBaseUrl);
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

        const int stage = 2;
        yield return ChallengeStage(stage, "demo-stage");
        if (!ShouldContinueDemo())
        {
            yield break;
        }

        yield return GetGameState();
        if (!ShouldContinueDemo())
        {
            yield break;
        }

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
        RefreshUi();
        lastRequestSucceeded = false;
        yield return request;
        isBusy = false;
        RefreshUi();
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
        RefreshUi();
    }

    private static string ReplayText(bool isReplay)
    {
        return isReplay ? " (replay)" : string.Empty;
    }

    private static string CreateIdempotencyKey(string keyPrefix)
    {
        return keyPrefix + "-" + Guid.NewGuid().ToString("N");
    }

    private void RefreshUi()
    {
        if (ui == null)
        {
            return;
        }

        ui.Refresh(apiBaseUrl, session, gameState, log, isBusy, isDemoRunning);
    }
}
