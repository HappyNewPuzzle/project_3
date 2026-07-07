using System;

[Serializable]
public sealed class IdleGuildApiResult<TResponse>
{
    public bool succeeded;
    public long statusCode;
    public string errorTitle;
    public TResponse response;

    public static IdleGuildApiResult<TResponse> Success(long statusCode, TResponse response)
    {
        return new IdleGuildApiResult<TResponse>
        {
            succeeded = true,
            statusCode = statusCode,
            response = response
        };
    }

    public static IdleGuildApiResult<TResponse> Failure(long statusCode, string errorTitle)
    {
        return new IdleGuildApiResult<TResponse>
        {
            succeeded = false,
            statusCode = statusCode,
            errorTitle = errorTitle
        };
    }
}

[Serializable]
public sealed class SystemStatusResponse
{
    public string status;
    public string serverTimeUtc;
}

[Serializable]
public sealed class GuestLoginResponse
{
    public string playerId;
    public string accessToken;
}

[Serializable]
public sealed class GameStateResponse
{
    public long gold;
    public int heroLevel;
    public int highestStage;
    public int productionBonusPercent;
}

[Serializable]
public sealed class ClaimIdleRewardResponse
{
    public long goldAwarded;
    public long goldBalanceAfter;
    public bool isReplay;
}

[Serializable]
public sealed class UpgradeHeroResponse
{
    public string outcome;
    public int heroLevelAfter;
    public long goldCost;
    public long goldBalanceAfter;
    public bool isReplay;
}

[Serializable]
public sealed class ChallengeStageResponse
{
    public string outcome;
    public int highestStageAfter;
    public int productionBonusPercentAfter;
    public bool isReplay;
}

[Serializable]
public sealed class ProblemDetails
{
    public string title;
}
