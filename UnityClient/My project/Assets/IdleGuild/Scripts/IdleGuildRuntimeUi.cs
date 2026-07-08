using System;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class IdleGuildRuntimeUi
{
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

    public void Build(
        Transform parent,
        string initialStage,
        Action onCheckStatus,
        Action onRunDemo,
        Action onGuestLogin,
        Action onGetGameState,
        Action onClaimReward,
        Action onUpgradeHero,
        Action<int> onChallengeStage,
        Action onClearSession)
    {
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("IdleGuild Runtime UI");
        canvasObject.transform.SetParent(parent, false);

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
        checkStatusButton = CreateButton(topButtons.transform, "Check Server", onCheckStatus);
        runDemoButton = CreateButton(topButtons.transform, "Run Demo Flow", onRunDemo);
        guestLoginButton = CreateButton(topButtons.transform, "1. Guest Login", onGuestLogin);

        GameObject actionButtons = CreateRow(panel.transform, "Action Buttons", 8f, 42f);
        getGameStateButton = CreateButton(actionButtons.transform, "2. State", onGetGameState);
        claimRewardButton = CreateButton(actionButtons.transform, "3. Claim", onClaimReward);
        upgradeHeroButton = CreateButton(actionButtons.transform, "4. Upgrade", onUpgradeHero);

        GameObject stageRow = CreateRow(panel.transform, "Stage Row", 8f, 42f);
        CreateText(stageRow.transform, "Stage", 15, FontStyle.Bold, TextAnchor.MiddleLeft, 42f, 70f);
        stageInputField = CreateInputField(stageRow.transform, initialStage);
        challengeStageButton = CreateButton(stageRow.transform, "5. Challenge Stage", () =>
        {
            onChallengeStage?.Invoke(ParseStageInput());
        });

        clearSessionButton = CreateButton(panel.transform, "Clear Saved Session", onClearSession);
        gameStateText = CreateText(panel.transform, string.Empty, 16, FontStyle.Normal, TextAnchor.UpperLeft, 112f);
        logText = CreateText(panel.transform, string.Empty, 14, FontStyle.Normal, TextAnchor.UpperLeft, 220f);
        logText.horizontalOverflow = HorizontalWrapMode.Wrap;
        logText.verticalOverflow = VerticalWrapMode.Truncate;
    }

    public void Refresh(
        string apiBaseUrl,
        IdleGuildSession session,
        GameStateResponse gameState,
        StringBuilder log,
        bool isBusy,
        bool isDemoRunning)
    {
        if (serverText == null)
        {
            return;
        }

        serverText.text = "Server: " + apiBaseUrl;
        playerText.text = "Player: " + (string.IsNullOrEmpty(session.PlayerId) ? "(not logged in)" : session.PlayerId);
        tokenText.text = "Token: " + (session.HasToken ? "saved" : "(none)");
        busyText.text = isDemoRunning ? "Status: running demo flow" : isBusy ? "Status: waiting for server" : "Status: ready";
        gameStateText.text = FormatGameState(gameState);
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

    private int ParseStageInput()
    {
        int stage;
        if (!int.TryParse(stageInputField.text, out stage) || stage < 1)
        {
            stage = 1;
            stageInputField.text = "1";
        }

        return stage;
    }

    private static string FormatGameState(GameStateResponse gameState)
    {
        if (gameState == null)
        {
            return "Game State\nNot loaded";
        }

        return
            "Game State\n" +
            "Gold: " + gameState.gold + "\n" +
            "Hero Level: " + gameState.heroLevel + "\n" +
            "Highest Stage: " + gameState.highestStage + "\n" +
            "Production Bonus: " + gameState.productionBonusPercent + "%";
    }

    private static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }

    private static GameObject CreatePanel(Transform parent, string objectName, Color color)
    {
        GameObject panel = new GameObject(objectName);
        panel.transform.SetParent(parent, false);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private static GameObject CreateRow(Transform parent, string objectName, float spacing, float height)
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

    private Button CreateButton(Transform parent, string label, Action onClick)
    {
        GameObject buttonObject = CreatePanel(parent, label + " Button", new Color(0.18f, 0.32f, 0.54f, 1f));
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        button.onClick.AddListener(() => onClick?.Invoke());

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
