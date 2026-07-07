using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    private Font uiFont;
    private Text serverText;
    private Text playerText;
    private Text tokenText;
    private Text busyText;
    private Text gameStateText;
    private Text logText;
    private InputField stageInputField;
    private Button checkStatusButton;
    private Button runDemoButton;
    private Button guestLoginButton;
    private Button getGameStateButton;
    private Button claimRewardButton;
    private Button upgradeHeroButton;
    private Button challengeStageButton;
    private Button clearSessionButton;

    private void Awake()
    {
        session.Load();
        api = new IdleGuildApiClient(() => session.AccessToken);
        BuildRuntimeUi();
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

        int stage = ParseStageInput();
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

    private void DrawGameState()
    {
        if (gameStateText == null)
        {
            return;
        }

        if (gameState == null)
        {
            gameStateText.text = "Game State\nNot loaded";
            return;
        }

        gameStateText.text =
            "Game State\n" +
            "Gold: " + gameState.gold + "\n" +
            "Hero Level: " + gameState.heroLevel + "\n" +
            "Highest Stage: " + gameState.highestStage + "\n" +
            "Production Bonus: " + gameState.productionBonusPercent + "%";
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

    private void BuildRuntimeUi()
    {
        uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("IdleGuild Runtime UI");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        Stretch(canvasRect);

        GameObject panel = CreatePanel(canvasObject.transform, "Panel", new Color(0.09f, 0.11f, 0.14f, 0.94f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        Stretch(panelRect);
        panelRect.offsetMin = new Vector2(28f, 28f);
        panelRect.offsetMax = new Vector2(-28f, -28f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 20, 20);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateText(panel.transform, "Idle Guild Unity Client", 26, FontStyle.Bold, TextAnchor.MiddleLeft, 36f);
        serverText = CreateText(panel.transform, string.Empty, 15, FontStyle.Normal, TextAnchor.MiddleLeft, 24f);
        playerText = CreateText(panel.transform, string.Empty, 15, FontStyle.Normal, TextAnchor.MiddleLeft, 24f);
        tokenText = CreateText(panel.transform, string.Empty, 15, FontStyle.Normal, TextAnchor.MiddleLeft, 24f);
        busyText = CreateText(panel.transform, string.Empty, 15, FontStyle.Bold, TextAnchor.MiddleLeft, 24f);

        GameObject topButtons = CreateRow(panel.transform, "Top Buttons", 8f, 42f);
        checkStatusButton = CreateButton(topButtons.transform, "Check Server", () => StartCoroutine(GetSystemStatus()));
        runDemoButton = CreateButton(topButtons.transform, "Run Demo Flow", () => StartCoroutine(RunDemoFlow()));
        guestLoginButton = CreateButton(topButtons.transform, "1. Guest Login", () => StartCoroutine(GuestLoginAndLoadState()));

        GameObject actionButtons = CreateRow(panel.transform, "Action Buttons", 8f, 42f);
        getGameStateButton = CreateButton(actionButtons.transform, "2. State", () => StartCoroutine(GetGameState()));
        claimRewardButton = CreateButton(actionButtons.transform, "3. Claim", () => StartCoroutine(ClaimIdleReward("idle-claim")));
        upgradeHeroButton = CreateButton(actionButtons.transform, "4. Upgrade", () => StartCoroutine(UpgradeMainHero("hero-upgrade")));

        GameObject stageRow = CreateRow(panel.transform, "Stage Row", 8f, 42f);
        CreateText(stageRow.transform, "Stage", 15, FontStyle.Bold, TextAnchor.MiddleLeft, 42f, 70f);
        stageInputField = CreateInputField(stageRow.transform, stageInput);
        stageInputField.onEndEdit.AddListener(value => stageInput = value);
        challengeStageButton = CreateButton(stageRow.transform, "5. Challenge Stage", () =>
        {
            stageInput = stageInputField.text;
            StartCoroutine(ChallengeStage(ParseStageInput(), "stage"));
        });

        clearSessionButton = CreateButton(panel.transform, "Clear Saved Session", ClearSession);
        gameStateText = CreateText(panel.transform, string.Empty, 16, FontStyle.Normal, TextAnchor.UpperLeft, 112f);
        logText = CreateText(panel.transform, string.Empty, 14, FontStyle.Normal, TextAnchor.UpperLeft, 220f);
        logText.horizontalOverflow = HorizontalWrapMode.Wrap;
        logText.verticalOverflow = VerticalWrapMode.Truncate;

        RefreshUi();
    }

    private void RefreshUi()
    {
        if (serverText == null)
        {
            return;
        }

        serverText.text = "Server: " + apiBaseUrl;
        playerText.text = "Player: " + (string.IsNullOrEmpty(session.PlayerId) ? "(not logged in)" : session.PlayerId);
        tokenText.text = "Token: " + (session.HasToken ? "saved" : "(none)");
        busyText.text = isDemoRunning ? "Status: running demo flow" : isBusy ? "Status: waiting for server" : "Status: ready";
        DrawGameState();
        logText.text = log.ToString();

        bool canUsePublicActions = !isBusy && !isDemoRunning;
        bool canUseProtectedActions = canUsePublicActions && session.HasToken;

        checkStatusButton.interactable = canUsePublicActions;
        runDemoButton.interactable = canUsePublicActions;
        guestLoginButton.interactable = canUsePublicActions;
        getGameStateButton.interactable = canUseProtectedActions;
        claimRewardButton.interactable = canUseProtectedActions;
        upgradeHeroButton.interactable = canUseProtectedActions;
        challengeStageButton.interactable = canUseProtectedActions;
        clearSessionButton.interactable = canUsePublicActions;
        stageInputField.interactable = canUsePublicActions;
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private GameObject CreatePanel(Transform parent, string objectName, Color color)
    {
        GameObject panel = new GameObject(objectName);
        panel.transform.SetParent(parent, false);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private GameObject CreateRow(Transform parent, string objectName, float spacing, float height)
    {
        GameObject row = new GameObject(objectName);
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        LayoutElement element = row.AddComponent<LayoutElement>();
        element.minHeight = height;
        element.preferredHeight = height;
        return row;
    }

    private Text CreateText(Transform parent, string value, int size, FontStyle style, TextAnchor alignment, float height, float width = -1f)
    {
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = uiFont;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = new Color(0.91f, 0.94f, 0.98f, 1f);

        LayoutElement element = textObject.AddComponent<LayoutElement>();
        element.minHeight = height;
        element.preferredHeight = height;
        if (width > 0f)
        {
            element.minWidth = width;
            element.preferredWidth = width;
            element.flexibleWidth = 0f;
        }

        return text;
    }

    private Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreatePanel(parent, label + " Button", new Color(0.18f, 0.32f, 0.54f, 1f));
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        button.onClick.AddListener(onClick);

        LayoutElement element = buttonObject.AddComponent<LayoutElement>();
        element.minHeight = 40f;
        element.preferredHeight = 40f;
        element.flexibleWidth = 1f;

        Text text = CreateText(buttonObject.transform, label, 14, FontStyle.Bold, TextAnchor.MiddleCenter, 40f);
        Stretch(text.rectTransform);
        return button;
    }

    private InputField CreateInputField(Transform parent, string value)
    {
        GameObject inputObject = CreatePanel(parent, "Stage Input", new Color(0.98f, 0.98f, 0.98f, 1f));
        InputField input = inputObject.AddComponent<InputField>();

        LayoutElement element = inputObject.AddComponent<LayoutElement>();
        element.minWidth = 90f;
        element.preferredWidth = 90f;
        element.flexibleWidth = 0f;

        Text text = CreateText(inputObject.transform, value, 16, FontStyle.Bold, TextAnchor.MiddleCenter, 40f);
        text.color = new Color(0.07f, 0.08f, 0.1f, 1f);
        Stretch(text.rectTransform);

        Text placeholder = CreateText(inputObject.transform, "2", 16, FontStyle.Normal, TextAnchor.MiddleCenter, 40f);
        placeholder.color = new Color(0.35f, 0.36f, 0.39f, 1f);
        Stretch(placeholder.rectTransform);

        input.textComponent = text;
        input.placeholder = placeholder;
        input.text = value;
        input.contentType = InputField.ContentType.IntegerNumber;
        return input;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
