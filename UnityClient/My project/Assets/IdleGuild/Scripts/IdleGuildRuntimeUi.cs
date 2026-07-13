using System;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Play 시 Canvas와 버튼을 코드로 만들어 디버그/데모 UI를 제공하는 클래스입니다.
public sealed class IdleGuildRuntimeUi
{
    // Unity 6에서 사용할 수 있는 기본 런타임 폰트입니다.
    private Font uiFont;
    // 서버 주소를 표시하는 텍스트입니다.
    private Text serverText;
    // 현재 세션의 playerId를 표시하는 텍스트입니다.
    private Text playerText;
    // accessToken 저장 여부를 표시하는 텍스트입니다.
    private Text tokenText;
    // 요청 진행 중/데모 실행 중/대기 상태를 표시하는 텍스트입니다.
    private Text busyText;
    // 서버에서 조회한 게임 상태를 표시하는 텍스트입니다.
    private Text gameStateText;
    // 최신 로그 메시지들을 표시하는 텍스트입니다.
    private Text logText;
    // 수동 스테이지 도전 번호를 입력받는 필드입니다.
    private InputField stageInputField;
    // 서버 상태 확인 버튼입니다.
    private Button checkStatusButton;
    // 전체 데모 플로우 실행 버튼입니다.
    private Button runDemoButton;
    // 게스트 로그인 버튼입니다.
    private Button guestLoginButton;
    // 게임 상태 조회 버튼입니다.
    private Button getGameStateButton;
    // 방치 보상 수령 버튼입니다.
    private Button claimRewardButton;
    // 영웅 강화 버튼입니다.
    private Button upgradeHeroButton;
    // 스테이지 도전 버튼입니다.
    private Button challengeStageButton;
    // 장비 목록 조회 후 청동 검을 장착하는 버튼입니다.
    private Button equipBronzeButton;
    // 상점의 작은 골드 팩을 모의 구매하는 버튼입니다.
    private Button buyGoldButton;
    // 저장된 세션 초기화 버튼입니다.
    private Button clearSessionButton;

    // 런타임 UI를 생성하고 각 버튼에 Bootstrap에서 넘겨준 콜백을 연결합니다.
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
        Action onEquipBronze,
        Action onBuyGold,
        Action onClearSession)
    {
        // Unity 6에서는 Arial.ttf 대신 LegacyRuntime.ttf를 기본 폰트로 사용해야 합니다.
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        // 버튼 클릭을 처리할 EventSystem이 없으면 새 Input System용으로 생성합니다.
        EnsureEventSystem();

        // 화면 전체를 덮는 Canvas 오브젝트를 만듭니다.
        GameObject canvasObject = new GameObject("IdleGuild Runtime UI");
        canvasObject.transform.SetParent(parent, false);

        // ScreenSpaceOverlay는 별도 카메라 없이 항상 화면 위에 UI를 그립니다.
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // 해상도에 따라 UI 크기가 자연스럽게 스케일되도록 기준 해상도를 설정합니다.
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        // 버튼 클릭과 입력 필드 포커스를 받기 위해 GraphicRaycaster가 필요합니다.
        canvasObject.AddComponent<GraphicRaycaster>();

        // Canvas RectTransform을 화면 전체로 늘립니다.
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        Stretch(canvasRect);

        // 어두운 반투명 배경 패널을 만들어 텍스트 가독성을 확보합니다.
        GameObject panel = CreatePanel(canvasObject.transform, "Panel", new Color(0.09f, 0.11f, 0.14f, 0.94f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.sizeDelta = new Vector2(500f, 0f);
        panelRect.offsetMin = new Vector2(-528f, 28f);
        panelRect.offsetMax = new Vector2(-28f, -28f);

        // VerticalLayoutGroup으로 텍스트와 버튼을 위에서 아래로 자동 배치합니다.
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 20, 20);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // 제목과 세션/서버 상태 텍스트를 생성합니다.
        CreateText(panel.transform, "Idle Guild Unity Client", 26, FontStyle.Bold, TextAnchor.MiddleLeft, 36f);
        serverText = CreateText(panel.transform, string.Empty, 15, FontStyle.Normal, TextAnchor.MiddleLeft, 24f);
        playerText = CreateText(panel.transform, string.Empty, 15, FontStyle.Normal, TextAnchor.MiddleLeft, 24f);
        tokenText = CreateText(panel.transform, string.Empty, 15, FontStyle.Normal, TextAnchor.MiddleLeft, 24f);
        busyText = CreateText(panel.transform, string.Empty, 15, FontStyle.Bold, TextAnchor.MiddleLeft, 24f);

        // 공개 API와 로그인 버튼을 첫 번째 줄에 배치합니다.
        GameObject topButtons = CreateRow(panel.transform, "Top Buttons", 8f, 42f);
        checkStatusButton = CreateButton(topButtons.transform, "Check Server", onCheckStatus);
        runDemoButton = CreateButton(topButtons.transform, "Run Demo Flow", onRunDemo);
        guestLoginButton = CreateButton(topButtons.transform, "1. Guest Login", onGuestLogin);

        // 인증이 필요한 주요 게임 액션 버튼을 두 번째 줄에 배치합니다.
        GameObject actionButtons = CreateRow(panel.transform, "Action Buttons", 8f, 42f);
        getGameStateButton = CreateButton(actionButtons.transform, "2. State", onGetGameState);
        claimRewardButton = CreateButton(actionButtons.transform, "3. Claim", onClaimReward);
        upgradeHeroButton = CreateButton(actionButtons.transform, "4. Upgrade", onUpgradeHero);

        // 스테이지 입력 필드와 도전 버튼을 세 번째 줄에 배치합니다.
        GameObject stageRow = CreateRow(panel.transform, "Stage Row", 8f, 42f);
        CreateText(stageRow.transform, "Stage", 15, FontStyle.Bold, TextAnchor.MiddleLeft, 42f, 70f);
        stageInputField = CreateInputField(stageRow.transform, initialStage);
        challengeStageButton = CreateButton(stageRow.transform, "5. Challenge Stage", () =>
        {
            // 버튼 클릭 시 입력 필드 값을 검증한 뒤 Bootstrap 콜백으로 전달합니다.
            onChallengeStage?.Invoke(ParseStageInput());
        });

        // 서버 고도화 기능인 장비와 모의 상점을 네 번째 줄에 배치합니다.
        GameObject extendedRow = CreateRow(panel.transform, "Extended Actions", 8f, 42f);
        equipBronzeButton = CreateButton(extendedRow.transform, "6. Equip Bronze", onEquipBronze);
        buyGoldButton = CreateButton(extendedRow.transform, "7. Buy 100 Gold (Mock)", onBuyGold);

        // 테스트 중 토큰이 꼬였을 때 바로 지울 수 있는 세션 초기화 버튼입니다.
        clearSessionButton = CreateButton(panel.transform, "Clear Saved Session", onClearSession);
        // 게임 상태와 로그 표시 영역을 생성합니다.
        gameStateText = CreateText(panel.transform, string.Empty, 16, FontStyle.Normal, TextAnchor.UpperLeft, 112f);
        logText = CreateText(panel.transform, string.Empty, 14, FontStyle.Normal, TextAnchor.UpperLeft, 220f);
        logText.horizontalOverflow = HorizontalWrapMode.Wrap;
        logText.verticalOverflow = VerticalWrapMode.Truncate;
    }

    // Bootstrap이 가진 최신 상태를 화면 텍스트와 버튼 활성화 상태에 반영합니다.
    public void Refresh(
        string apiBaseUrl,
        IdleGuildSession session,
        GameStateResponse gameState,
        StringBuilder log,
        bool isBusy,
        bool isDemoRunning)
    {
        // Build 전에 Refresh가 호출되는 상황을 방어합니다.
        if (serverText == null)
        {
            return;
        }

        // 서버 주소, 플레이어, 토큰, 진행 상태 텍스트를 갱신합니다.
        serverText.text = "Server: " + apiBaseUrl;
        playerText.text = "Player: " + (string.IsNullOrEmpty(session.PlayerId) ? "(not logged in)" : session.PlayerId);
        tokenText.text = "Token: " + (session.HasToken ? "saved" : "(none)");
        busyText.text = isDemoRunning ? "Status: running demo flow" : isBusy ? "Status: waiting for server" : "Status: ready";
        // 게임 상태와 로그 본문을 갱신합니다.
        gameStateText.text = FormatGameState(gameState);
        logText.text = log.ToString();

        // 서버 요청 중이거나 데모 실행 중이면 중복 클릭을 막습니다.
        bool canUsePublicActions = !isBusy && !isDemoRunning;
        // 보호 API 버튼은 토큰이 있을 때만 활성화합니다.
        bool canUseProtectedActions = canUsePublicActions && session.HasToken;

        // 공개 액션 버튼 활성화 상태를 반영합니다.
        checkStatusButton.interactable = canUsePublicActions;
        runDemoButton.interactable = canUsePublicActions;
        guestLoginButton.interactable = canUsePublicActions;
        // 인증이 필요한 게임 액션 버튼 활성화 상태를 반영합니다.
        getGameStateButton.interactable = canUseProtectedActions;
        claimRewardButton.interactable = canUseProtectedActions;
        upgradeHeroButton.interactable = canUseProtectedActions;
        challengeStageButton.interactable = canUseProtectedActions;
        equipBronzeButton.interactable = canUseProtectedActions;
        buyGoldButton.interactable = canUseProtectedActions;
        // 세션 초기화와 스테이지 입력은 요청 중이 아닐 때만 허용합니다.
        clearSessionButton.interactable = canUsePublicActions;
        stageInputField.interactable = canUsePublicActions;
    }

    // 스테이지 입력 필드 값을 양의 정수로 변환합니다.
    private int ParseStageInput()
    {
        // 파싱 실패나 1 미만 값은 안전하게 1스테이지로 보정합니다.
        int stage;
        if (!int.TryParse(stageInputField.text, out stage) || stage < 1)
        {
            stage = 1;
            stageInputField.text = "1";
        }

        // 검증된 스테이지 번호를 반환합니다.
        return stage;
    }

    // GameStateResponse를 화면 표시용 여러 줄 문자열로 변환합니다.
    private static string FormatGameState(GameStateResponse gameState)
    {
        // 아직 상태를 조회하지 않았으면 비어 있음을 명확히 표시합니다.
        if (gameState == null)
        {
            return "Game State\nNot loaded";
        }

        // 서버가 내려준 핵심 값을 디버그 패널에 표시합니다.
        return
            "Game State\n" +
            "Gold: " + gameState.gold + "\n" +
            "Hero Level: " + gameState.heroLevel + "\n" +
            "Hero Power: " + gameState.heroPower + " (equipment +" + gameState.equipmentPowerBonus + ")\n" +
            "Highest Stage: " + gameState.highestStage + "\n" +
            "Production Bonus: " + gameState.productionBonusPercent + "%";
    }

    // UI 클릭 처리를 위한 EventSystem을 생성합니다.
    private static void EnsureEventSystem()
    {
        // 씬에 이미 EventSystem이 있으면 중복 생성하지 않습니다.
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        // 프로젝트가 새 Input System을 사용하므로 InputSystemUIInputModule을 붙입니다.
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }

    // 배경색이 있는 UI 패널 오브젝트를 생성합니다.
    private static GameObject CreatePanel(Transform parent, string objectName, Color color)
    {
        // 빈 GameObject를 만들고 지정한 부모 아래에 배치합니다.
        GameObject panel = new GameObject(objectName);
        panel.transform.SetParent(parent, false);
        // Image 컴포넌트를 붙여 배경색이 보이게 합니다.
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    // 버튼들을 한 줄로 배치하기 위한 HorizontalLayoutGroup 행을 생성합니다.
    private static GameObject CreateRow(Transform parent, string objectName, float spacing, float height)
    {
        // 행 역할을 하는 빈 GameObject를 생성합니다.
        GameObject row = new GameObject(objectName);
        row.transform.SetParent(parent, false);

        // 자식 버튼들이 가로로 배치되도록 레이아웃 그룹을 설정합니다.
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        // 행의 고정 높이를 지정해 버튼 줄이 흔들리지 않게 합니다.
        LayoutElement element = row.AddComponent<LayoutElement>();
        element.minHeight = height;
        element.preferredHeight = height;
        return row;
    }

    // 지정한 스타일과 크기의 Text UI를 생성합니다.
    private Text CreateText(Transform parent, string value, int size, FontStyle style, TextAnchor alignment, float height, float width = -1f)
    {
        // 텍스트 전용 GameObject를 생성합니다.
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(parent, false);

        // Unity 기본 Text 컴포넌트를 설정합니다.
        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = uiFont;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = new Color(0.91f, 0.94f, 0.98f, 1f);

        // 레이아웃 시스템이 텍스트 높이와 선택적 너비를 계산하도록 설정합니다.
        LayoutElement element = textObject.AddComponent<LayoutElement>();
        element.minHeight = height;
        element.preferredHeight = height;
        if (width > 0f)
        {
            element.minWidth = width;
            element.preferredWidth = width;
            element.flexibleWidth = 0f;
        }

        // 생성된 Text 컴포넌트를 반환해 호출자가 내용을 갱신할 수 있게 합니다.
        return text;
    }

    // 지정한 라벨과 클릭 콜백을 가진 버튼을 생성합니다.
    private Button CreateButton(Transform parent, string label, Action onClick)
    {
        // 버튼 배경은 Image가 있는 패널로 만들고 Button 컴포넌트를 추가합니다.
        GameObject buttonObject = CreatePanel(parent, label + " Button", new Color(0.18f, 0.32f, 0.54f, 1f));
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        // UnityEvent는 Action을 직접 받지 않으므로 람다로 감싸 호출합니다.
        button.onClick.AddListener(() => onClick?.Invoke());

        // 버튼 높이와 가로 확장을 설정합니다.
        LayoutElement element = buttonObject.AddComponent<LayoutElement>();
        element.minHeight = 40f;
        element.preferredHeight = 40f;
        element.flexibleWidth = 1f;

        // 버튼 중앙에 라벨 텍스트를 채웁니다.
        Text text = CreateText(buttonObject.transform, label, 14, FontStyle.Bold, TextAnchor.MiddleCenter, 40f);
        Stretch(text.rectTransform);
        return button;
    }

    // 스테이지 번호 입력을 위한 InputField를 생성합니다.
    private InputField CreateInputField(Transform parent, string value)
    {
        // 흰색 배경을 가진 입력 필드 오브젝트를 생성합니다.
        GameObject inputObject = CreatePanel(parent, "Stage Input", new Color(0.98f, 0.98f, 0.98f, 1f));
        InputField input = inputObject.AddComponent<InputField>();

        // 입력 필드는 버튼보다 좁은 고정 폭을 사용합니다.
        LayoutElement element = inputObject.AddComponent<LayoutElement>();
        element.minWidth = 90f;
        element.preferredWidth = 90f;
        element.flexibleWidth = 0f;

        // 실제 입력값을 표시하는 Text를 생성합니다.
        Text text = CreateText(inputObject.transform, value, 16, FontStyle.Bold, TextAnchor.MiddleCenter, 40f);
        text.color = new Color(0.07f, 0.08f, 0.1f, 1f);
        Stretch(text.rectTransform);

        // 값이 비어 있을 때 보여줄 placeholder Text를 생성합니다.
        Text placeholder = CreateText(inputObject.transform, "2", 16, FontStyle.Normal, TextAnchor.MiddleCenter, 40f);
        placeholder.color = new Color(0.35f, 0.36f, 0.39f, 1f);
        Stretch(placeholder.rectTransform);

        // InputField가 어떤 Text를 편집하고 어떤 placeholder를 쓸지 연결합니다.
        input.textComponent = text;
        input.placeholder = placeholder;
        input.text = value;
        input.contentType = InputField.ContentType.IntegerNumber;
        return input;
    }

    // RectTransform을 부모 영역 전체에 맞게 늘립니다.
    private static void Stretch(RectTransform rectTransform)
    {
        // anchor를 전체 영역으로 설정합니다.
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        // offset을 0으로 만들어 부모와 같은 크기를 사용하게 합니다.
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
