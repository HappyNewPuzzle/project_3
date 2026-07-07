using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public sealed class IdleGuildApiClient
{
    private readonly Func<string> getAccessToken;

    public IdleGuildApiClient(Func<string> getAccessToken)
    {
        this.getAccessToken = getAccessToken;
    }

    public IEnumerator GetSystemStatus(string apiBaseUrl, Action<IdleGuildApiResult<SystemStatusResponse>> onComplete)
    {
        yield return Send(
            apiBaseUrl,
            UnityWebRequest.kHttpVerbGET,
            "/api/v1/system/status",
            null,
            false,
            onComplete);
    }

    public IEnumerator GuestLogin(string apiBaseUrl, Action<IdleGuildApiResult<GuestLoginResponse>> onComplete)
    {
        yield return Send(
            apiBaseUrl,
            UnityWebRequest.kHttpVerbPOST,
            "/api/v1/accounts/guest",
            null,
            false,
            onComplete);
    }

    public IEnumerator GetGameState(string apiBaseUrl, Action<IdleGuildApiResult<GameStateResponse>> onComplete)
    {
        yield return Send(
            apiBaseUrl,
            UnityWebRequest.kHttpVerbGET,
            "/api/v1/game-state",
            null,
            true,
            onComplete);
    }

    public IEnumerator ClaimIdleReward(string apiBaseUrl, string idempotencyKey, Action<IdleGuildApiResult<ClaimIdleRewardResponse>> onComplete)
    {
        yield return Send(
            apiBaseUrl,
            UnityWebRequest.kHttpVerbPOST,
            "/api/v1/rewards/idle/claim",
            idempotencyKey,
            true,
            onComplete);
    }

    public IEnumerator UpgradeMainHero(string apiBaseUrl, string idempotencyKey, Action<IdleGuildApiResult<UpgradeHeroResponse>> onComplete)
    {
        yield return Send(
            apiBaseUrl,
            UnityWebRequest.kHttpVerbPOST,
            "/api/v1/heroes/main/upgrade",
            idempotencyKey,
            true,
            onComplete);
    }

    public IEnumerator ChallengeStage(string apiBaseUrl, int stage, string idempotencyKey, Action<IdleGuildApiResult<ChallengeStageResponse>> onComplete)
    {
        yield return Send(
            apiBaseUrl,
            UnityWebRequest.kHttpVerbPOST,
            "/api/v1/stages/" + stage + "/challenge",
            idempotencyKey,
            true,
            onComplete);
    }

    private IEnumerator Send<TResponse>(
        string apiBaseUrl,
        string method,
        string path,
        string idempotencyKey,
        bool includeAuthorization,
        Action<IdleGuildApiResult<TResponse>> onComplete)
    {
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

            string accessToken = getAccessToken();
            if (includeAuthorization && !string.IsNullOrWhiteSpace(accessToken))
            {
                request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            }

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                request.SetRequestHeader("Idempotency-Key", idempotencyKey);
            }

            yield return request.SendWebRequest();

            string body = request.downloadHandler.text;
            if (request.result != UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(IdleGuildApiResult<TResponse>.Failure(request.responseCode, ParseErrorTitle(body)));
                yield break;
            }

            TResponse response = JsonUtility.FromJson<TResponse>(body);
            onComplete?.Invoke(IdleGuildApiResult<TResponse>.Success(request.responseCode, response));
        }
    }

    private static string ParseErrorTitle(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return "Request failed without response body.";
        }

        try
        {
            ProblemDetails problem = JsonUtility.FromJson<ProblemDetails>(body);
            return problem == null || string.IsNullOrWhiteSpace(problem.title) ? body : problem.title;
        }
        catch (Exception)
        {
            return body;
        }
    }
}
