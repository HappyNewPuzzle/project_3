using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class IdleGuildIdleHud : MonoBehaviour
{
    private IdleGuildProgression progression;
    private Font font;
    private Text stageText;
    private Text currencyText;
    private Text powerText;
    private Text ratesText;
    private Text bossText;
    private Image bossFill;
    private GameObject bossPanel;
    private Text toastText;
    private Text growthText;
    private GameObject growthPanel;
    private GameObject characterPanel;
    private GameObject equipmentPanel;
    private GameObject offlinePanel;
    private GameObject skillPanel;
    private GameObject settingsPanel;
    private GameObject missionPanel;
    private GameObject tutorialPanel;
    private Text missionText;
    private Text tutorialText;
    private int tutorialPage;
    private Text equipmentText;
    private Text skillCooldownText;
    private Action<int> skillAction;
    private Action offlineClaimAction;

    public void Build(
        IdleGuildProgression value,
        Action onSelectGirl,
        Action onSelectCat,
        Action onSelectClassic)
    {
        progression = value;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Canvas canvas = gameObject.AddComponent<Canvas>();
        gameObject.AddComponent<IdleGuildSafeArea>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        GameObject top = Panel(transform, "Top HUD", new Color(0.04f, 0.08f, 0.12f, 0.88f));
        SetRect(top, new Vector2(0.02f, 0.89f), new Vector2(0.98f, 0.98f), Vector2.zero, Vector2.zero);
        HorizontalLayoutGroup topLayout = top.AddComponent<HorizontalLayoutGroup>();
        topLayout.padding = new RectOffset(24, 24, 8, 8);
        topLayout.spacing = 25f;
        topLayout.childAlignment = TextAnchor.MiddleCenter;
        stageText = Label(top.transform, "", 24, FontStyle.Bold);
        currencyText = Label(top.transform, "", 21, FontStyle.Bold);
        powerText = Label(top.transform, "", 21, FontStyle.Bold);
        ratesText = Label(top.transform, "", 18, FontStyle.Normal);

        bossPanel = Panel(transform, "Boss HUD", new Color(0.12f, 0.03f, 0.04f, 0.9f));
        SetRect(bossPanel, new Vector2(0.25f, 0.76f), new Vector2(0.75f, 0.86f), Vector2.zero, Vector2.zero);
        bossText = Label(bossPanel.transform, "", 22, FontStyle.Bold);
        SetRect(bossText.gameObject, new Vector2(0f, 0.48f), Vector2.one, new Vector2(14f, 0f), new Vector2(-14f, -4f));
        GameObject bar = Panel(bossPanel.transform, "Boss Bar", new Color(0.15f, 0.15f, 0.16f, 1f));
        SetRect(bar, new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.42f), Vector2.zero, Vector2.zero);
        bossFill = Panel(bar.transform, "Fill", new Color(0.9f, 0.16f, 0.12f, 1f)).GetComponent<Image>();
        SetRect(bossFill.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        bossFill.type = Image.Type.Filled;
        bossFill.fillMethod = Image.FillMethod.Horizontal;
        bossPanel.SetActive(false);

        growthPanel = Panel(transform, "Growth", new Color(0.04f, 0.07f, 0.1f, 0.97f));
        SetRect(growthPanel, new Vector2(0.32f, 0.25f), new Vector2(0.68f, 0.72f), Vector2.zero, Vector2.zero);
        VerticalLayoutGroup growthLayout = growthPanel.AddComponent<VerticalLayoutGroup>();
        growthLayout.padding = new RectOffset(12, 12, 9, 9);
        growthLayout.spacing = 5f;
        Label(growthPanel.transform, "영웅 성장", 23, FontStyle.Bold);
        growthText = Label(growthPanel.transform, "", 17, FontStyle.Bold);
        Button(growthPanel.transform, "공격력 강화", () => TryUpgrade(progression.UpgradeAttack, "공격력이 상승했습니다!"));
        Button(growthPanel.transform, "공격속도 강화", () => TryUpgrade(progression.UpgradeSpeed, "공격속도가 상승했습니다!"));
        Button(growthPanel.transform, "치명타 강화", () => TryUpgrade(progression.UpgradeCritical, "치명타 확률이 상승했습니다!"));
        Button(growthPanel.transform, "닫기", CloseAllPanels);
        growthPanel.SetActive(false);

        GameObject bottom = Panel(transform, "Bottom Menu", new Color(0.04f, 0.08f, 0.12f, 0.94f));
        SetRect(bottom, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.14f), Vector2.zero, Vector2.zero);
        GridLayoutGroup bottomLayout = bottom.AddComponent<GridLayoutGroup>();
        bottomLayout.padding = new RectOffset(18, 18, 7, 7);
        bottomLayout.spacing = new Vector2(8f, 5f);
        bottomLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        bottomLayout.constraintCount = 4;
        bottomLayout.cellSize = new Vector2(285f, 34f);
        Button(bottom.transform, "성장", ToggleGrowthPanel);
        Button(bottom.transform, "영웅", ToggleCharacterPanel);
        Button(bottom.transform, "장비", ToggleEquipmentPanel);
        Button(bottom.transform, "스킬", ToggleSkillPanel);
        Button(bottom.transform, "던전", () => { progression.CompleteDungeon(); ShowToast("던전 보상: 보석 +3"); });
        Button(bottom.transform, "상점", () => TryUpgrade(progression.UpgradeCritical, "치명타 상품을 구매했습니다!"));
        Button(bottom.transform, "설정", ToggleSettingsPanel);
        Button(bottom.transform, "임무", ToggleMissionPanel);

        characterPanel = Panel(transform, "Character Selection", new Color(0.05f, 0.09f, 0.14f, 0.96f));
        SetRect(characterPanel, new Vector2(0.33f, 0.27f), new Vector2(0.67f, 0.70f), Vector2.zero, Vector2.zero);
        VerticalLayoutGroup characterLayout = characterPanel.AddComponent<VerticalLayoutGroup>();
        characterLayout.padding = new RectOffset(14, 14, 10, 10);
        characterLayout.spacing = 7f;
        Label(characterPanel.transform, "주인공 선택", 21, FontStyle.Bold);
        Button(characterPanel.transform, "소녀", onSelectGirl);
        Button(characterPanel.transform, "검은 고양이", onSelectCat);
        Button(characterPanel.transform, "클래식", onSelectClassic);
        Button(characterPanel.transform, "닫기", CloseAllPanels);
        characterPanel.SetActive(false);

        equipmentPanel = Panel(transform, "Equipment Inventory", new Color(0.05f, 0.09f, 0.14f, 0.97f));
        SetRect(equipmentPanel, new Vector2(0.28f, 0.17f), new Vector2(0.72f, 0.82f), Vector2.zero, Vector2.zero);
        VerticalLayoutGroup equipmentLayout = equipmentPanel.AddComponent<VerticalLayoutGroup>();
        equipmentLayout.padding = new RectOffset(14, 14, 10, 10);
        equipmentLayout.spacing = 7f;
        Label(equipmentPanel.transform, "장비 인벤토리", 21, FontStyle.Bold);
        equipmentText = Label(equipmentPanel.transform, "", 17, FontStyle.Normal);
        Button(equipmentPanel.transform, "최고 장비 자동 장착", () => TryUpgrade(progression.AutoEquip, "최고 장비를 장착했습니다"));
        Button(equipmentPanel.transform, "여분 장비 판매", () => TryUpgrade(progression.SellSpareEquipment, "여분 장비를 판매했습니다"));
        Button(equipmentPanel.transform, "여분 장비 분해", () => TryUpgrade(progression.DisassembleSpareEquipment, "장비 재료를 획득했습니다"));
        Button(equipmentPanel.transform, "동일 장비 3개 합성", () => TryUpgrade(progression.FuseEquipment, "장비 등급이 상승했습니다"));
        Button(equipmentPanel.transform, "환생", () => TryUpgrade(progression.Prestige, "환생 완료! 영구 공격력이 상승했습니다"));
        Button(equipmentPanel.transform, "닫기", CloseAllPanels);
        equipmentPanel.SetActive(false);

        skillPanel = Panel(transform, "Skill Loadout", new Color(0.05f, 0.09f, 0.14f, 0.97f));
        SetRect(skillPanel, new Vector2(0.28f, 0.14f), new Vector2(0.72f, 0.84f), Vector2.zero, Vector2.zero);
        VerticalLayoutGroup skillLayout = skillPanel.AddComponent<VerticalLayoutGroup>();
        skillLayout.padding = new RectOffset(14, 14, 10, 10);
        skillLayout.spacing = 7f;
        Label(skillPanel.transform, "장착 스킬 3개", 21, FontStyle.Bold);
        Button(skillPanel.transform, "STAR BURST", () => UseSkill(0));
        Button(skillPanel.transform, "SWIFT STRIKE", () => UseSkill(1));
        Button(skillPanel.transform, "GUARDIAN LIGHT", () => UseSkill(2));
        Button(skillPanel.transform, "STAR BURST 강화", () => TryUpgrade(() => progression.UpgradeSkill(0), "스킬 레벨이 올랐습니다"));
        Button(skillPanel.transform, "SWIFT STRIKE 강화", () => TryUpgrade(() => progression.UpgradeSkill(1), "스킬 레벨이 올랐습니다"));
        Button(skillPanel.transform, "GUARDIAN LIGHT 강화", () => TryUpgrade(() => progression.UpgradeSkill(2), "스킬 레벨이 올랐습니다"));
        Button(skillPanel.transform, "닫기", CloseAllPanels);
        skillPanel.SetActive(false);

        settingsPanel = Panel(transform, "Settings and Rewards", new Color(0.05f, 0.09f, 0.14f, 0.97f));
        SetRect(settingsPanel, new Vector2(0.33f, 0.27f), new Vector2(0.67f, 0.72f), Vector2.zero, Vector2.zero);
        VerticalLayoutGroup settingsLayout = settingsPanel.AddComponent<VerticalLayoutGroup>();
        settingsLayout.padding = new RectOffset(14, 14, 10, 10);
        settingsLayout.spacing = 7f;
        Label(settingsPanel.transform, "설정 / 보상", 21, FontStyle.Bold);
        Button(settingsPanel.transform, "사운드 켜기/끄기", () => { IdleGuildReleaseServices.ToggleSound(); ShowToast(IdleGuildReleaseServices.SoundEnabled ? "사운드 ON" : "사운드 OFF"); });
        Button(settingsPanel.transform, "오늘의 출석 보상", () => TryUpgrade(progression.ClaimDailyAttendance, "출석 보상을 받았습니다"));
        Button(settingsPanel.transform, "우편함 환영 보상", () => TryUpgrade(progression.ClaimWelcomeMail, "우편 보상을 받았습니다"));
        Button(settingsPanel.transform, "닫기", CloseAllPanels);
        settingsPanel.SetActive(false);

        missionPanel = Panel(transform, "Missions", new Color(0.05f, 0.09f, 0.14f, 0.98f));
        SetRect(missionPanel, new Vector2(0.28f, 0.12f), new Vector2(0.72f, 0.84f), Vector2.zero, Vector2.zero);
        VerticalLayoutGroup missionLayout = missionPanel.AddComponent<VerticalLayoutGroup>();
        missionLayout.padding = new RectOffset(14, 14, 10, 10);
        missionLayout.spacing = 6f;
        Label(missionPanel.transform, "오늘의 임무 / 업적", 21, FontStyle.Bold);
        missionText = Label(missionPanel.transform, string.Empty, 16, FontStyle.Normal);
        Button(missionPanel.transform, "몬스터 20마리 보상", () => ClaimMission(0));
        Button(missionPanel.transform, "보스 처치 보상", () => ClaimMission(1));
        Button(missionPanel.transform, "공격력 3회 강화 보상", () => ClaimMission(2));
        Button(missionPanel.transform, "누적 처치 업적", () => ClaimAchievement(0));
        Button(missionPanel.transform, "스테이지 10 업적", () => ClaimAchievement(1));
        Button(missionPanel.transform, "장비 5개 업적", () => ClaimAchievement(2));
        Button(missionPanel.transform, "닫기", CloseAllPanels);
        missionPanel.SetActive(false);

        skillCooldownText = Label(transform, "", 20, FontStyle.Bold);
        skillCooldownText.alignment = TextAnchor.MiddleCenter;
        SetRect(skillCooldownText.gameObject, new Vector2(0.72f, 0.66f), new Vector2(0.96f, 0.72f), Vector2.zero, Vector2.zero);

        CreateOfflineRewardPanel();

        toastText = Label(transform, "", 26, FontStyle.Bold);
        toastText.alignment = TextAnchor.MiddleCenter;
        toastText.color = new Color(1f, 0.9f, 0.3f, 1f);
        SetRect(toastText.gameObject, new Vector2(0.3f, 0.58f), new Vector2(0.7f, 0.66f), Vector2.zero, Vector2.zero);
        toastText.gameObject.SetActive(false);

        progression.Changed += Refresh;
        Refresh();
        CreateTutorial();
    }

    public void SetBoss(bool visible, int health, int maxHealth, float seconds)
    {
        bossPanel.SetActive(visible);
        if (!visible) return;
        bossFill.fillAmount = maxHealth <= 0 ? 0f : Mathf.Clamp01((float)health / maxHealth);
        bossText.text = seconds >= 0f
            ? "BOSS  HP " + health + "/" + maxHealth + "     ⏱ " + seconds.ToString("0.0") + "s"
            : "MONSTER  HP " + health + "/" + maxHealth;
    }

    public void ShowGold(int amount)
    {
        ShowToast("+" + amount + " GOLD");
    }

    public void SetSkillAction(Action<int> action)
    {
        skillAction = action;
    }

    public void SetOfflineClaimAction(Action action)
    {
        offlineClaimAction = action;
    }

    public void ShowToast(string message)
    {
        StopAllCoroutines();
        if (toastText != null) toastText.transform.SetAsLastSibling();
        StartCoroutine(Toast(message));
    }

    private IEnumerator Toast(string message)
    {
        toastText.text = message;
        toastText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.1f);
        toastText.gameObject.SetActive(false);
    }

    private void TryUpgrade(Func<bool> action, string success)
    {
        ShowToast(action() ? success : "골드가 부족합니다");
    }

    private void ToggleGrowthPanel()
    {
        ToggleExclusivePanel(growthPanel);
    }

    private void ToggleCharacterPanel()
    {
        ToggleExclusivePanel(characterPanel);
    }

    private void ToggleEquipmentPanel()
    {
        ToggleExclusivePanel(equipmentPanel);
    }

    private void UseSkill(int skillIndex)
    {
        if (!progression.UseSkill(skillIndex))
        {
            ShowToast("스킬 재사용 대기 중 " + progression.SkillCooldownRemaining.ToString("0.0") + "초");
            return;
        }

        // 성공한 스킬 입력은 패널을 먼저 닫아 전투 캐릭터와 스킬 연출이 가려지지 않게 합니다.
        CloseAllPanels();
        skillAction?.Invoke(skillIndex);
        string[] names = { "STAR BURST!", "SWIFT STRIKE!", "GUARDIAN LIGHT!" };
        ShowToast(names[Mathf.Clamp(skillIndex, 0, names.Length - 1)]);
    }

    private void ToggleSkillPanel()
    {
        ToggleExclusivePanel(skillPanel);
    }

    private void ToggleSettingsPanel()
    {
        ToggleExclusivePanel(settingsPanel);
    }

    private void ToggleMissionPanel()
    {
        ToggleExclusivePanel(missionPanel);
    }

    private void ToggleExclusivePanel(GameObject target)
    {
        bool shouldOpen = target != null && !target.activeSelf;
        CloseAllPanels();
        if (!shouldOpen) return;
        target.SetActive(true);
        target.transform.SetAsLastSibling();
    }

    private void CloseAllPanels()
    {
        if (growthPanel != null) growthPanel.SetActive(false);
        if (characterPanel != null) characterPanel.SetActive(false);
        if (equipmentPanel != null) equipmentPanel.SetActive(false);
        if (skillPanel != null) skillPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (missionPanel != null) missionPanel.SetActive(false);
    }

    private void ClaimMission(int mission)
    {
        ShowToast(progression.ClaimDailyMission(mission) ? "임무 보상을 받았습니다" : "아직 완료되지 않았거나 이미 받았습니다");
    }

    private void ClaimAchievement(int achievement)
    {
        ShowToast(progression.ClaimAchievement(achievement) ? "업적 보상을 받았습니다" : "아직 완료되지 않았거나 이미 받았습니다");
    }

    private void CreateTutorial()
    {
        if (PlayerPrefs.GetInt("IdleGuild.TutorialCompleted", 0) == 1) return;
        tutorialPanel = Panel(transform, "First Run Tutorial", new Color(0.02f, 0.04f, 0.08f, 0.97f));
        SetRect(tutorialPanel, new Vector2(0.24f, 0.26f), new Vector2(0.76f, 0.72f), Vector2.zero, Vector2.zero);
        VerticalLayoutGroup layout = tutorialPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 24, 24);
        layout.spacing = 14f;
        Label(tutorialPanel.transform, "IDLE GUILD 모험 안내", 28, FontStyle.Bold);
        tutorialText = Label(tutorialPanel.transform, string.Empty, 21, FontStyle.Normal);
        Button(tutorialPanel.transform, "다음", AdvanceTutorial);
        tutorialPage = 0;
        RefreshTutorial();
    }

    private void AdvanceTutorial()
    {
        tutorialPage++;
        if (tutorialPage < 3)
        {
            RefreshTutorial();
            return;
        }
        PlayerPrefs.SetInt("IdleGuild.TutorialCompleted", 1);
        PlayerPrefs.Save();
        tutorialPanel.SetActive(false);
        ShowToast("모험을 시작합니다!");
    }

    private void RefreshTutorial()
    {
        string[] pages =
        {
            "영웅은 제자리에서 달리며 몬스터를 자동으로 공격합니다.\n몬스터를 쓰러뜨려 골드와 장비를 획득하세요.",
            "왼쪽 성장 버튼으로 공격력·공격속도·치명타를 강화하세요.\n스킬 메뉴에서는 세 가지 액티브 스킬을 사용할 수 있습니다.",
            "보스에게 승리하면 다음 스테이지와 지역이 열립니다.\n임무·출석·우편 보상도 잊지 말고 받아가세요."
        };
        tutorialText.text = pages[Mathf.Clamp(tutorialPage, 0, pages.Length - 1)];
    }

    private void Update()
    {
        if (progression == null || skillCooldownText == null) return;
        skillCooldownText.text = progression.IsSkillReady
            ? "SKILL READY"
            : "SKILL " + progression.SkillCooldownRemaining.ToString("0.0") + "s";
    }

    private void CreateOfflineRewardPanel()
    {
        if (progression.PendingOfflineGold <= 0) return;
        offlinePanel = Panel(transform, "Offline Reward", new Color(0.03f, 0.06f, 0.1f, 0.98f));
        SetRect(offlinePanel, new Vector2(0.3f, 0.33f), new Vector2(0.7f, 0.68f), Vector2.zero, Vector2.zero);
        VerticalLayoutGroup layout = offlinePanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(22, 22, 18, 18);
        layout.spacing = 10f;
        Label(offlinePanel.transform, "방치 보상", 28, FontStyle.Bold);
        TimeSpan elapsed = TimeSpan.FromSeconds(progression.OfflineSeconds);
        Label(offlinePanel.transform,
            string.Format("{0}시간 {1}분 동안\n{2} GOLD 획득", (int)elapsed.TotalHours, elapsed.Minutes, progression.PendingOfflineGold),
            21,
            FontStyle.Normal);
        Button(offlinePanel.transform, "받기", () => ClaimOffline(1));
        Button(offlinePanel.transform, "2배 받기", () => ClaimOffline(2));
    }

    private void ClaimOffline(int multiplier)
    {
        int amount = progression.ClaimOfflineReward(multiplier);
        offlineClaimAction?.Invoke();
        offlinePanel.SetActive(false);
        ShowToast("방치 보상 +" + amount + " GOLD");
    }

    private void Refresh()
    {
        stageText.text = "STAGE " + progression.Stage;
        currencyText.text = "GOLD " + progression.Gold + "     GEM " + progression.Gems;
        powerText.text = "전투력 " + progression.CombatPower;
        ratesText.text = "초당 골드 " + progression.GoldPerSecond.ToString("0.0") + "  |  초당 공격력 " + progression.DamagePerSecond;
        growthText.text = "성장  ATK Lv." + progression.AttackLevel + " / SPD Lv." + progression.SpeedLevel + " / CRIT Lv." + progression.CriticalLevel + "\n장비 Tier " + progression.EquipmentTier;
        if (equipmentText != null)
        {
            equipmentText.text = "보유 " + progression.EquipmentCount + "개 / 재료 " + progression.EquipmentMaterials + "\n장착: " + progression.EquippedItemName + "\n" + progression.EquipmentRarity + " / " + progression.EquipmentSlot + "\n스킬 Lv." + progression.SkillOneLevel + "/" + progression.SkillTwoLevel + "/" + progression.SkillThreeLevel;
        }
        if (missionText != null)
        {
            missionText.text = "몬스터 " + progression.DailyKillProgress + "/20\n보스 " + progression.DailyBossProgress + "/1\n공격 강화 " + progression.DailyUpgradeProgress + "/3\n누적: 처치 " + progression.DefeatedMonsters + " / 스테이지 " + progression.Stage + " / 장비 " + progression.EquipmentCount;
        }
    }

    private GameObject Panel(Transform parent, string name, Color color)
    {
        GameObject target = new GameObject(name, typeof(RectTransform), typeof(Image));
        target.transform.SetParent(parent, false);
        target.GetComponent<Image>().color = color;
        return target;
    }

    private Text Label(Transform parent, string value, int size, FontStyle style)
    {
        GameObject target = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        target.transform.SetParent(parent, false);
        Text label = target.GetComponent<Text>();
        label.font = font;
        label.text = value;
        label.fontSize = size;
        label.fontStyle = style;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        target.GetComponent<LayoutElement>().flexibleWidth = 1f;
        return label;
    }

    private void Button(Transform parent, string title, Action action)
    {
        GameObject target = Panel(parent, title, new Color(0.16f, 0.36f, 0.56f, 1f));
        UnityEngine.UI.Button button = target.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = target.GetComponent<Image>();
        button.onClick.AddListener(() => action());
        button.onClick.AddListener(() => IdleGuildReleaseServices.PlayEffect(620f));
        LayoutElement element = target.AddComponent<LayoutElement>();
        element.minHeight = 34f;
        element.flexibleWidth = 1f;
        Text text = Label(target.transform, title, 17, FontStyle.Bold);
        SetRect(text.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    private static void SetRect(GameObject target, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
