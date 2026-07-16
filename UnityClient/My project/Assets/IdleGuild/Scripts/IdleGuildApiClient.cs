using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public interface IIdleGuildApiClient
{
    IEnumerator GetSystemStatus(string apiBaseUrl, Action<IdleGuildApiResult<SystemStatusResponse>> onComplete);
    IEnumerator GuestLogin(string apiBaseUrl, Action<IdleGuildApiResult<GuestLoginResponse>> onComplete);
    IEnumerator GetGameState(string apiBaseUrl, Action<IdleGuildApiResult<GameStateResponse>> onComplete);
    IEnumerator SyncProgression(string apiBaseUrl, SyncProgressionRequest progression, Action<IdleGuildApiResult<SyncProgressionResponse>> onComplete);
    IEnumerator ClaimIdleReward(string apiBaseUrl, string idempotencyKey, Action<IdleGuildApiResult<ClaimIdleRewardResponse>> onComplete);
    IEnumerator UpgradeMainHero(string apiBaseUrl, string idempotencyKey, Action<IdleGuildApiResult<UpgradeHeroResponse>> onComplete);
    IEnumerator ChallengeStage(string apiBaseUrl, int stage, string idempotencyKey, Action<IdleGuildApiResult<ChallengeStageResponse>> onComplete);
    IEnumerator GetEquipment(string apiBaseUrl, Action<IdleGuildApiResult<EquipmentInventoryResponse>> onComplete);
    IEnumerator Equip(string apiBaseUrl, string equipmentId, string idempotencyKey, Action<IdleGuildApiResult<ChangeEquipmentResponse>> onComplete);
    IEnumerator GetShopProducts(string apiBaseUrl, Action<IdleGuildApiResult<ShopCatalogResponse>> onComplete);
    IEnumerator Purchase(string apiBaseUrl, string productId, string idempotencyKey, Action<IdleGuildApiResult<ShopPurchaseResponse>> onComplete);
}

// Idle Guild 서버 MVP HTTP API를 UnityWebRequest 코루틴으로 호출하는 전담 클라이언트입니다.
public sealed class IdleGuildApiClient : IIdleGuildApiClient
{
    // 요청 직전에 최신 accessToken을 가져오기 위한 함수입니다.
    private readonly Func<string> getAccessToken;

    // 세션 객체를 직접 소유하지 않고 토큰 조회 함수만 받아 결합도를 낮춥니다.
    public IdleGuildApiClient(Func<string> getAccessToken)
    {
        // Bootstrap이 넘겨준 함수는 보호 API 요청 시 Authorization 헤더 값을 만드는 데 사용됩니다.
        this.getAccessToken = getAccessToken;
    }

    // 서버 상태 확인 API를 호출합니다.
    public IEnumerator GetSystemStatus(string apiBaseUrl, Action<IdleGuildApiResult<SystemStatusResponse>> onComplete)
    {
        // 공개 API이므로 Authorization 헤더 없이 GET으로 호출합니다.
        yield return Send(
            apiBaseUrl,
            UnityWebRequest.kHttpVerbGET,
            "/api/v1/system/status",
            null,
            false,
            onComplete);
    }

    // 게스트 계정 생성 및 JWT 발급 API를 호출합니다.
    public IEnumerator GuestLogin(string apiBaseUrl, Action<IdleGuildApiResult<GuestLoginResponse>> onComplete)
    {
        // 로그인 API는 토큰을 만들기 전 단계라 인증 헤더를 붙이지 않습니다.
        yield return Send(
            apiBaseUrl,
            UnityWebRequest.kHttpVerbPOST,
            "/api/v1/accounts/guest",
            null,
            false,
            onComplete);
    }

    // 현재 플레이어의 게임 상태 API를 호출합니다.
    public IEnumerator GetGameState(string apiBaseUrl, Action<IdleGuildApiResult<GameStateResponse>> onComplete)
    {
        // 보호 API이므로 includeAuthorization을 true로 넘깁니다.
        yield return Send(
            apiBaseUrl,
            UnityWebRequest.kHttpVerbGET,
            "/api/v1/game-state",
            null,
            true,
            onComplete);
    }

    public IEnumerator SyncProgression(string apiBaseUrl, SyncProgressionRequest progression, Action<IdleGuildApiResult<SyncProgressionResponse>> onComplete)
    {
        yield return Send(
            apiBaseUrl,
            UnityWebRequest.kHttpVerbPUT,
            "/api/v1/game-state/progression",
            null,
            true,
            onComplete,
            JsonUtility.ToJson(progression));
    }

    // 방치 보상 수령 API를 호출합니다.
    public IEnumerator ClaimIdleReward(string apiBaseUrl, string idempotencyKey, Action<IdleGuildApiResult<ClaimIdleRewardResponse>> onComplete)
    {
        // 상태 변경 API이므로 서버 요구사항에 맞춰 Idempotency-Key를 전달합니다.
        yield return Send(
            apiBaseUrl,
            UnityWebRequest.kHttpVerbPOST,
            "/api/v1/rewards/idle/claim",
            idempotencyKey,
            true,
            onComplete);
    }

    // 메인 영웅 강화 API를 호출합니다.
    public IEnumerator UpgradeMainHero(string apiBaseUrl, string idempotencyKey, Action<IdleGuildApiResult<UpgradeHeroResponse>> onComplete)
    {
        // 강화는 골드 차감이 발생하므로 멱등 키와 인증 토큰을 함께 보냅니다.
        yield return Send(
            apiBaseUrl,
            UnityWebRequest.kHttpVerbPOST,
            "/api/v1/heroes/main/upgrade",
            idempotencyKey,
            true,
            onComplete);
    }

    // 지정한 스테이지 도전 API를 호출합니다.
    public IEnumerator ChallengeStage(string apiBaseUrl, int stage, string idempotencyKey, Action<IdleGuildApiResult<ChallengeStageResponse>> onComplete)
    {
        // URL 경로에 스테이지 번호를 넣어 서버가 해당 스테이지 전투를 판정하게 합니다.
        yield return Send(
            apiBaseUrl,
            UnityWebRequest.kHttpVerbPOST,
            "/api/v1/stages/" + stage + "/challenge",
            idempotencyKey,
            true,
            onComplete);
    }

    // 플레이어가 보유한 장비와 현재 장착 상태를 조회합니다.
    public IEnumerator GetEquipment(string apiBaseUrl, Action<IdleGuildApiResult<EquipmentInventoryResponse>> onComplete)
    {
        yield return Send(apiBaseUrl, UnityWebRequest.kHttpVerbGET, "/api/v1/equipment", null, true, onComplete);
    }

    // 지정한 보유 장비를 장착하고 같은 슬롯의 기존 장비를 교체합니다.
    public IEnumerator Equip(string apiBaseUrl, string equipmentId, string idempotencyKey, Action<IdleGuildApiResult<ChangeEquipmentResponse>> onComplete)
    {
        yield return Send(apiBaseUrl, UnityWebRequest.kHttpVerbPUT,
            "/api/v1/equipment/" + equipmentId + "/equipped", idempotencyKey, true, onComplete,
            "{\"isEquipped\":true}");
    }

    // 서버가 판매 중인 모의 상품 카탈로그를 조회합니다.
    public IEnumerator GetShopProducts(string apiBaseUrl, Action<IdleGuildApiResult<ShopCatalogResponse>> onComplete)
    {
        yield return Send(apiBaseUrl, UnityWebRequest.kHttpVerbGET, "/api/v1/shop/products", null, true, onComplete);
    }

    // 서버 카탈로그의 상품을 모의 구매하고 골드를 지급받습니다.
    public IEnumerator Purchase(string apiBaseUrl, string productId, string idempotencyKey, Action<IdleGuildApiResult<ShopPurchaseResponse>> onComplete)
    {
        yield return Send(apiBaseUrl, UnityWebRequest.kHttpVerbPOST,
            "/api/v1/shop/products/" + productId + "/purchase", idempotencyKey, true, onComplete);
    }

    // 모든 HTTP 요청이 공통으로 사용하는 내부 전송 메서드입니다.
    private IEnumerator Send<TResponse>(
        string apiBaseUrl,
        string method,
        string path,
        string idempotencyKey,
        bool includeAuthorization,
        Action<IdleGuildApiResult<TResponse>> onComplete,
        string jsonBody = null)
    {
        // base URL 끝의 슬래시를 정리한 뒤 API path를 붙여 최종 요청 URL을 만듭니다.
        string url = apiBaseUrl.TrimEnd('/') + path;

        // UnityWebRequest는 using으로 감싸 요청 객체와 핸들러가 정리되게 합니다.
        using (UnityWebRequest request = new UnityWebRequest(url, method))
        {
            request.timeout = 12;
            // 응답 본문을 문자열로 읽기 위해 DownloadHandlerBuffer를 사용합니다.
            request.downloadHandler = new DownloadHandlerBuffer();

            // POST 요청은 본문이 비어 있어도 UploadHandler가 있어야 안정적으로 전송됩니다.
            if (method == UnityWebRequest.kHttpVerbPOST || method == UnityWebRequest.kHttpVerbPUT)
            {
                // 서버 API는 현재 별도 요청 body가 없으므로 빈 byte 배열을 보냅니다.
                request.uploadHandler = new UploadHandlerRaw(
                    string.IsNullOrEmpty(jsonBody) ? new byte[0] : System.Text.Encoding.UTF8.GetBytes(jsonBody));
                // 서버가 JSON API로 처리하도록 Content-Type을 명시합니다.
                request.SetRequestHeader("Content-Type", "application/json");
            }

            // 모든 API에서 JSON 응답을 기대한다고 서버에 알립니다.
            request.SetRequestHeader("Accept", "application/json");

            // 보호 API라면 세션에서 최신 accessToken을 꺼냅니다.
            string accessToken = getAccessToken();
            // 토큰이 있을 때만 Authorization: Bearer 헤더를 붙입니다.
            if (includeAuthorization && !string.IsNullOrWhiteSpace(accessToken))
            {
                request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            }

            // 상태 변경 API에는 중복 요청 방지를 위한 멱등 키를 붙입니다.
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                request.SetRequestHeader("Idempotency-Key", idempotencyKey);
            }

            // Unity 코루틴에서 네트워크 요청이 끝날 때까지 기다립니다.
            yield return request.SendWebRequest();

            // 성공/실패 모두 응답 body를 읽어 이후 처리에 사용합니다.
            string body = request.downloadHandler.text;
            // 서버 관측성 헤더를 성공과 실패 결과에 모두 보존합니다.
            string traceId = request.GetResponseHeader("X-Trace-Id");
            // UnityWebRequest가 네트워크 오류나 HTTP 오류로 판단한 경우 실패 결과를 콜백합니다.
            if (request.result != UnityWebRequest.Result.Success)
            {
                ProblemDetails problem = ParseProblem(body);
                onComplete?.Invoke(IdleGuildApiResult<TResponse>.Failure(
                    request.responseCode,
                    problem == null || string.IsNullOrWhiteSpace(problem.title)
                        ? string.IsNullOrWhiteSpace(body) ? request.error : body
                        : problem.title,
                    string.IsNullOrWhiteSpace(traceId) ? problem?.traceId : traceId));
                yield break;
            }

            // 성공 응답 JSON을 Unity JsonUtility로 DTO 객체에 매핑합니다.
            TResponse response = JsonUtility.FromJson<TResponse>(body);
            // 호출자에게 HTTP 상태 코드와 파싱된 응답 DTO를 전달합니다.
            onComplete?.Invoke(IdleGuildApiResult<TResponse>.Success(request.responseCode, response, traceId));
        }
    }

    // ProblemDetails 또는 일반 응답 body에서 사람이 읽을 오류 제목을 추출합니다.
    private static ProblemDetails ParseProblem(string body)
    {
        // 응답 body가 비어 있으면 네트워크/서버 레벨 오류로 보고 기본 메시지를 반환합니다.
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            // 서버 MVP는 오류를 ProblemDetails 형태로 내려주므로 title 필드를 우선 사용합니다.
            return JsonUtility.FromJson<ProblemDetails>(body);
        }
        catch (Exception)
        {
            // JSON이 아닌 HTML/프록시 오류 등이 오면 원문 body를 그대로 보여줍니다.
            return null;
        }
    }
}
