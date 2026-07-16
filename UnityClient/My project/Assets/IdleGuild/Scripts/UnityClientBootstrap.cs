using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

// MainScene에서 실행되는 클라이언트 진입점이며, 서버 API 호출 흐름과 런타임 UI를 연결합니다.
public sealed class UnityClientBootstrap : MonoBehaviour
{
    public enum CharacterVisualSet
    {
        CuteGirlAndMaskedThief,
        ClassicHeroAndSlime,
        BlackCatAndMaskedThief
    }

    // Unity Inspector에서 바꿀 수 있는 서버 주소이며, 로컬 서버 기본 포트는 5219입니다.
    [SerializeField] private string apiBaseUrl = "http://localhost:5219";
    [SerializeField] private bool useMockApi = true;
    [SerializeField] private CharacterVisualSet characterVisualSet = CharacterVisualSet.CuteGirlAndMaskedThief;
    [SerializeField] private IdleGuildBattleSceneLayout battleSceneLayout;
    [SerializeField] private bool showDeveloperPanel;

    // 게스트 로그인 토큰과 playerId를 PlayerPrefs에 저장하고 불러오는 세션 객체입니다.
    private readonly IdleGuildSession session = new IdleGuildSession();
    // 화면 로그 영역에 보여줄 메시지를 최신순으로 쌓는 버퍼입니다.
    private readonly StringBuilder log = new StringBuilder();

    // HTTP 요청을 실제로 보내는 API 클라이언트입니다.
    private IIdleGuildApiClient api;
    // Play 시 코드로 생성되는 버튼/텍스트 기반 런타임 UI입니다.
    private IdleGuildRuntimeUi ui;
    private IdleGuildGameWorld gameWorld;
    private IdleGuildProgression progression;
    private IdleGuildIdleHud idleHud;
    // 스테이지 입력의 기본값이며, 수동 도전 UI에 전달됩니다.
    private string stageInput = "2";
    // 서버 요청이 진행 중인지 표시해서 중복 클릭을 막는 상태값입니다.
    private bool isBusy;
    // 자동 데모 플로우가 실행 중인지 표시해서 다른 버튼 입력을 잠그는 상태값입니다.
    private bool isDemoRunning;
    // 마지막 API 요청 성공 여부를 저장해 자동 데모 중간 실패 시 즉시 멈추게 합니다.
    private bool lastRequestSucceeded;
    // 서버에서 받아온 최신 게임 상태이며, UI의 Gold/Level/Stage 표시로 사용됩니다.
    private GameStateResponse gameState;
    private int huntGold;
    private Coroutine progressionSyncRoutine;
    private int syncedAttackLevel;
    private int syncedSpeedLevel;
    private int syncedCriticalLevel;
    private int syncedPrestigeLevel;
    private int syncedSoulStones;
    private int syncedEquipmentTier;
    private int syncedEquipmentCount;
    private int syncedUnlockedRegion;
    private int syncedSkillOneLevel;
    private int syncedSkillTwoLevel;
    private int syncedSkillThreeLevel;
    private const string CharacterVisualSetKey = "IdleGuild.CharacterVisualSet";
    private const string SkipOfflineRewardOnceKey = "IdleGuild.SkipOfflineRewardOnce";

    // Unity 오브젝트가 활성화될 때 세션을 불러오고 API/UI 객체를 준비합니다.
    private void Awake()
    {
#if IDLE_GUILD_SERVER_BUILD && UNITY_ANDROID
        useMockApi = false;
        apiBaseUrl = "http://10.0.2.2:5219";
#elif IDLE_GUILD_SERVER_BUILD
        useMockApi = false;
#endif
        IdleGuildReleaseServices.Install(gameObject);
        characterVisualSet = (CharacterVisualSet)PlayerPrefs.GetInt(
            CharacterVisualSetKey,
            (int)characterVisualSet);
        // 이전 Play에서 저장된 게스트 토큰과 playerId를 복원합니다.
        session.Load();
        // API 클라이언트가 요청 직전에 최신 토큰을 읽을 수 있도록 세션 접근 함수를 넘깁니다.
        if (useMockApi)
        {
            api = new IdleGuildMockApiClient();
        }
        else
        {
            api = new IdleGuildApiClient(() => session.AccessToken);
        }
        // UI 생성과 버튼 콜백 연결을 전담 객체에 맡겨 Bootstrap의 책임을 줄입니다.
        ui = new IdleGuildRuntimeUi();
        if (battleSceneLayout == null)
        {
            battleSceneLayout = FindFirstObjectByType<IdleGuildBattleSceneLayout>();
        }
        // 영웅 변경은 Scene을 다시 로드하지만 실제 앱 재접속은 아니므로 방치 보상을 한 번만 건너뜁니다.
        bool skipOfflineReward = PlayerPrefs.GetInt(SkipOfflineRewardOnceKey, 0) == 1;
        if (skipOfflineReward)
        {
            PlayerPrefs.DeleteKey(SkipOfflineRewardOnceKey);
            PlayerPrefs.Save();
        }
        progression = new IdleGuildProgression(calculateOfflineReward: !skipOfflineReward);
        RememberSyncedProgression();
        progression.Changed += OnProgressionChanged;
        GameObject hudObject = new GameObject("Idle Guild Game HUD");
        hudObject.transform.SetParent(transform, false);
        idleHud = hudObject.AddComponent<IdleGuildIdleHud>();
        idleHud.Build(
            progression,
            () => SelectCharacter(CharacterVisualSet.CuteGirlAndMaskedThief),
            () => SelectCharacter(CharacterVisualSet.BlackCatAndMaskedThief),
            () => SelectCharacter(CharacterVisualSet.ClassicHeroAndSlime));
        gameWorld = new IdleGuildGameWorld(
            transform,
            this,
            characterVisualSet != CharacterVisualSet.ClassicHeroAndSlime,
            characterVisualSet == CharacterVisualSet.BlackCatAndMaskedThief,
            battleSceneLayout,
            progression,
            idleHud);
        gameWorld.Build();
        idleHud.SetSkillAction(gameWorld.ActivateSkill);
        idleHud.SetOfflineClaimAction(() =>
        {
            if (session.HasToken && !isBusy)
            {
                StartCoroutine(ClaimIdleReward("game-offline-claim"));
            }
        });
        // 각 버튼이 눌렸을 때 실행할 코루틴/액션을 런타임 UI에 연결합니다.
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
            () => StartCoroutine(EquipBronzeSword()),
            () => StartCoroutine(BuySmallGoldPack()),
            () => SelectCharacter(CharacterVisualSet.CuteGirlAndMaskedThief),
            () => SelectCharacter(CharacterVisualSet.BlackCatAndMaskedThief),
            () => SelectCharacter(CharacterVisualSet.ClassicHeroAndSlime),
            ClearSession);
        ui.SetVisible(showDeveloperPanel);
        gameWorld.StartAutoHunt(OnHuntGoldEarned, OnAutoBossDefeated);
        if (session.HasToken)
        {
            StartCoroutine(RestoreSessionWithRetry());
        }
        // 초기 상태를 로그에 남기고 UI를 한 번 갱신합니다.
        AddLog("Ready. Mode: " + (useMockApi ? "Mock API" : "Server API") + ", Server: " + apiBaseUrl);
    }

    // 서버가 살아 있는지 확인하는 공개 상태 API를 호출합니다.
    private IEnumerator GetSystemStatus()
    {
        // 공통 요청 래퍼로 busy 상태를 관리하면서 system/status 요청을 실행합니다.
        yield return RunRequest(
            api.GetSystemStatus(apiBaseUrl, result =>
            {
                // 실패 응답이면 로그를 남기고 성공 처리 로직을 건너뜁니다.
                if (HandleFailure(result))
                {
                    return;
                }

                // 서버 상태와 UTC 시간을 로그로 보여줘 연결 여부를 즉시 확인하게 합니다.
                AddLog("Server status: " + result.response.status + " at " + result.response.serverTimeUtc);
            }));
    }

    // 문서의 MVP 순서대로 서버 기능을 한 번에 실행하는 자동 데모 플로우입니다.
    private IEnumerator RunDemoFlow()
    {
        // 자동 데모 중에는 다른 버튼 입력을 막습니다.
        isDemoRunning = true;
        AddLog("Demo flow started.");

        // 1단계: 서버 상태를 확인해 서버가 꺼져 있으면 바로 멈춥니다.
        yield return GetSystemStatus();
        if (!ShouldContinueDemo())
        {
            yield break;
        }

        // 2단계: 게스트 계정을 생성하고 accessToken을 저장합니다.
        yield return GuestLogin();
        if (!ShouldContinueDemo())
        {
            yield break;
        }

        // 3단계: 생성된 계정의 초기 게임 상태를 가져옵니다.
        yield return GetGameState();
        if (!ShouldContinueDemo())
        {
            yield break;
        }

        // 서버의 방치 보상 계산을 눈으로 보기 위해 잠시 기다립니다.
        AddLog("Waiting 10 seconds for idle reward...");
        yield return new WaitForSeconds(10f);

        // 4단계: 방치 보상을 수령합니다.
        yield return ClaimIdleReward("demo-idle-claim");
        if (!ShouldContinueDemo())
        {
            yield break;
        }

        // 5단계: 메인 영웅 강화를 요청합니다.
        yield return UpgradeMainHero("demo-hero-upgrade");
        if (!ShouldContinueDemo())
        {
            yield break;
        }

        // 6단계: 서버 문서의 기본 시나리오인 스테이지 2에 도전합니다.
        const int stage = 2;
        yield return ChallengeStage(stage, "demo-stage");
        if (!ShouldContinueDemo())
        {
            yield break;
        }

        // 7단계: 최종 게임 상태를 다시 조회해 보상/강화/스테이지 결과를 확인합니다.
        yield return GetGameState();
        if (!ShouldContinueDemo())
        {
            yield break;
        }

        // 모든 단계가 성공하면 데모 완료 로그를 남기고 버튼 잠금을 해제합니다.
        AddLog("Demo flow finished.");
        isDemoRunning = false;
    }

    // 게스트 로그인 API를 호출하고 성공 시 세션에 토큰과 playerId를 저장합니다.
    private IEnumerator GuestLogin()
    {
        // 로그인 API는 토큰이 없어도 호출할 수 있으므로 API 클라이언트가 인증 헤더를 붙이지 않습니다.
        yield return RunRequest(
            api.GuestLogin(apiBaseUrl, result =>
            {
                // 실패 응답이면 ProblemDetails 제목을 로그에 남기고 저장을 건너뜁니다.
                if (HandleFailure(result))
                {
                    return;
                }

                // 성공 응답의 accessToken과 playerId를 PlayerPrefs에 저장합니다.
                session.Save(result.response);
                AddLog("Guest login succeeded. Player: " + session.PlayerId);
            }));
    }

    // 수동 로그인 버튼에서 사용하는 흐름이며, 로그인 성공 직후 상태 조회까지 이어갑니다.
    private IEnumerator GuestLoginAndLoadState()
    {
        // 먼저 게스트 로그인을 수행합니다.
        yield return GuestLogin();
        // 로그인 성공 시에만 보호 API인 game-state를 호출합니다.
        if (lastRequestSucceeded)
        {
            yield return GetGameState();
        }
    }

    // 현재 플레이어의 서버 권위 게임 상태를 조회합니다.
    private IEnumerator GetGameState()
    {
        // accessToken이 필요한 보호 API이므로 세션 토큰이 Authorization 헤더로 들어갑니다.
        yield return RunRequest(
            api.GetGameState(apiBaseUrl, result =>
            {
                // 인증 실패나 서버 오류가 있으면 로그만 남기고 상태 갱신은 하지 않습니다.
                if (HandleFailure(result))
                {
                    return;
                }

                // 성공 응답을 로컬 캐시에 보관하고 UI가 최신 값을 표시하게 합니다.
                gameState = result.response;
                syncedAttackLevel = Mathf.Max(gameState.heroLevel, gameState.attackLevel);
                syncedSpeedLevel = gameState.attackSpeedLevel;
                syncedCriticalLevel = gameState.criticalLevel;
                syncedPrestigeLevel = gameState.prestigeLevel;
                syncedSoulStones = gameState.soulStones;
                syncedEquipmentTier = gameState.equipmentTier;
                syncedEquipmentCount = gameState.equipmentCount;
                syncedUnlockedRegion = gameState.unlockedRegion;
                syncedSkillOneLevel = gameState.skillOneLevel;
                syncedSkillTwoLevel = gameState.skillTwoLevel;
                syncedSkillThreeLevel = gameState.skillThreeLevel;
                progression.ApplyServerState(gameState);
                AddLog("State: gold " + gameState.gold + ", hero " + gameState.heroLevel + ", stage " + gameState.highestStage + ", bonus " + gameState.productionBonusPercent + "%");
            }));
    }

    private IEnumerator RestoreSessionWithRetry()
    {
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            yield return GetGameState();
            if (lastRequestSucceeded) yield break;
            AddLog("Reconnect attempt " + attempt + "/3 failed.");
            yield return new WaitForSeconds(attempt * 1.5f);
        }

        AddLog("Server reconnect failed. Local progress remains available.");
    }

    // 방치 보상 수령 API를 호출합니다.
    private IEnumerator ClaimIdleReward(string keyPrefix)
    {
        // 상태 변경 API이므로 중복 요청 방지를 위해 매번 새로운 Idempotency-Key를 붙입니다.
        yield return RunRequest(
            api.ClaimIdleReward(apiBaseUrl, CreateIdempotencyKey(keyPrefix), result =>
            {
                // 실패 시 서버가 내려준 오류 제목을 로그로 표시합니다.
                if (HandleFailure(result))
                {
                    return;
                }

                // 성공 시 지급 골드와 지급 후 잔액을 로그에 남깁니다.
                AddLog("Idle reward: +" + result.response.goldAwarded + " gold, balance " + result.response.goldBalanceAfter + ReplayText(result.response.isReplay));
            }));

        // 수동 실행일 때는 결과 반영을 위해 곧바로 게임 상태를 다시 가져옵니다.
        if (!isDemoRunning && lastRequestSucceeded)
        {
            yield return GetGameState();
        }
    }

    // 메인 영웅 강화 API를 호출합니다.
    private IEnumerator UpgradeMainHero(string keyPrefix)
    {
        // 강화도 상태 변경 API라서 Idempotency-Key를 생성해 서버에 보냅니다.
        yield return RunRequest(
            api.UpgradeMainHero(apiBaseUrl, CreateIdempotencyKey(keyPrefix), result =>
            {
                // 골드 부족 같은 게임 규칙 실패도 서버 응답으로 받아 로그 처리합니다.
                if (HandleFailure(result))
                {
                    return;
                }

                // 강화 결과, 강화 후 레벨, 소모 골드를 한 줄로 표시합니다.
                AddLog("Hero upgrade: " + result.response.outcome + ", level " + result.response.heroLevelAfter + ", cost " + result.response.goldCost + ReplayText(result.response.isReplay));
            }));

        // 수동 실행일 때는 강화 결과가 상태 패널에 즉시 보이도록 재조회합니다.
        if (!isDemoRunning && lastRequestSucceeded)
        {
            yield return GetGameState();
        }
    }

    // 지정한 스테이지에 도전하는 API를 호출합니다.
    private IEnumerator ChallengeStage(int stage, string keyPrefix)
    {
        // 스테이지 번호와 멱등 키를 함께 보내 서버가 도전 결과를 결정하게 합니다.
        yield return RunRequest(
            api.ChallengeStage(apiBaseUrl, stage, CreateIdempotencyKey(keyPrefix + "-" + stage), result =>
            {
                // 패배, 잠금, 서버 오류 등은 여기서 로그로 표시됩니다.
                if (HandleFailure(result))
                {
                    return;
                }

                // 도전 결과와 최고 스테이지, 생산 보너스 변화를 로그에 남깁니다.
                AddLog("Stage " + stage + ": " + result.response.outcome + ", highest " + result.response.highestStageAfter + ", bonus " + result.response.productionBonusPercentAfter + "%" + ReplayText(result.response.isReplay));
                gameWorld.PlayStageChallenge(stage, result.response.outcome);
            }));

        // 수동 도전일 때는 최종 상태를 다시 조회해 화면에 반영합니다.
        if (!isDemoRunning && lastRequestSucceeded)
        {
            yield return GetGameState();
        }
    }

    // 보유 장비를 조회하고 청동 검이 있으면 서버에 장착을 요청합니다.
    private IEnumerator EquipBronzeSword()
    {
        EquipmentInventoryResponse inventory = null;
        yield return RunRequest(api.GetEquipment(apiBaseUrl, result =>
        {
            if (HandleFailure(result)) return;
            inventory = result.response;
        }));

        if (!lastRequestSucceeded || inventory == null || inventory.items == null) yield break;
        EquipmentItemResponse bronze = Array.Find(inventory.items, item => item.definitionId == "bronze-sword");
        if (bronze == null)
        {
            AddLog("Bronze Sword is not owned.");
            yield break;
        }

        yield return RunRequest(api.Equip(apiBaseUrl, bronze.equipmentId,
            CreateIdempotencyKey("equip-bronze"), result =>
            {
                if (HandleFailure(result)) return;
                AddLog("Equipment: " + result.response.outcome + ReplayText(result.response.isReplay));
            }));
        if (lastRequestSucceeded) yield return GetGameState();
    }

    // 서버 상품 카탈로그를 확인한 뒤 작은 골드 팩을 모의 구매합니다.
    private IEnumerator BuySmallGoldPack()
    {
        ShopCatalogResponse catalog = null;
        yield return RunRequest(api.GetShopProducts(apiBaseUrl, result =>
        {
            if (HandleFailure(result)) return;
            catalog = result.response;
        }));

        if (!lastRequestSucceeded || catalog == null || catalog.products == null) yield break;
        ShopProductResponse product = Array.Find(catalog.products, item => item.productId == "small-gold-pack");
        if (product == null)
        {
            AddLog("Small Gold Pack is not available.");
            yield break;
        }

        yield return RunRequest(api.Purchase(apiBaseUrl, product.productId,
            CreateIdempotencyKey("shop-small-gold"), result =>
            {
                if (HandleFailure(result)) return;
                AddLog("Mock purchase: +" + result.response.goldAwarded +
                    " gold, balance " + result.response.goldBalanceAfter + ReplayText(result.response.isReplay));
            }));
        if (lastRequestSucceeded) yield return GetGameState();
    }

    // 모든 API 코루틴을 감싸 busy 플래그와 UI 갱신을 공통 처리합니다.
    private IEnumerator RunRequest(IEnumerator request)
    {
        // 요청 시작 시 버튼을 비활성화하고 상태 텍스트를 waiting으로 바꿉니다.
        isBusy = true;
        RefreshUi();
        // 새 요청의 결과가 나오기 전까지는 실패로 간주해 데모가 성급히 계속되지 않게 합니다.
        lastRequestSucceeded = false;
        // 실제 API 요청 코루틴을 실행합니다.
        yield return request;
        // 요청 완료 후 버튼을 다시 활성화하고 최신 상태를 UI에 반영합니다.
        isBusy = false;
        RefreshUi();
    }

    // API 결과가 실패인지 확인하고 실패면 로그를 남깁니다.
    private bool HandleFailure<TResponse>(IdleGuildApiResult<TResponse> result)
    {
        // 자동 데모가 다음 단계로 넘어갈지 판단할 수 있도록 마지막 요청 결과를 저장합니다.
        lastRequestSucceeded = result.succeeded;
        // 성공이면 호출자가 성공 응답을 처리하도록 false를 반환합니다.
        if (result.succeeded)
        {
            return false;
        }

        // 실패이면 HTTP 상태 코드와 서버 오류 제목을 로그에 남깁니다.
        AddLog("HTTP " + result.statusCode + ": " + result.errorTitle +
            (string.IsNullOrWhiteSpace(result.traceId) ? string.Empty : " (trace " + result.traceId + ")"));
        return true;
    }

    // 저장된 세션과 화면 상태를 초기화합니다.
    private void ClearSession()
    {
        // PlayerPrefs에 저장된 토큰/playerId를 삭제합니다.
        session.Clear();
        // 이전 플레이어의 게임 상태가 화면에 남지 않도록 로컬 캐시도 비웁니다.
        gameState = null;
        AddLog("Saved session cleared.");
    }

    // 자동 데모 중 마지막 요청 성공 여부를 보고 계속 진행할지 결정합니다.
    private bool ShouldContinueDemo()
    {
        // 마지막 요청이 성공이면 다음 데모 단계로 이동합니다.
        if (lastRequestSucceeded)
        {
            return true;
        }

        // 실패 시 데모 모드를 해제하고 중단 로그를 남깁니다.
        AddLog("Demo flow stopped.");
        isDemoRunning = false;
        return false;
    }

    // 로그 버퍼에 메시지를 추가하고 UI를 즉시 갱신합니다.
    private void AddLog(string message)
    {
        // 새 로그가 위에 오도록 문자열의 앞쪽에 삽입합니다.
        log.Insert(0, "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + Environment.NewLine);
        // 로그가 바뀌었으므로 화면 텍스트도 즉시 다시 그립니다.
        RefreshUi();
    }

    // 서버가 같은 멱등 키를 재처리했는지 로그에 짧게 표시합니다.
    private static string ReplayText(bool isReplay)
    {
        // isReplay가 true이면 서버가 이전 결과를 재사용했다는 의미입니다.
        return isReplay ? " (replay)" : string.Empty;
    }

    private void OnHuntGoldEarned(int amount)
    {
        huntGold += amount;
        RefreshUi();
    }

    private void OnProgressionChanged()
    {
        if (!session.HasToken || ProgressionMatchesLastSync() || progressionSyncRoutine != null)
        {
            return;
        }

        progressionSyncRoutine = StartCoroutine(SyncProgressionAfterDelay());
    }

    private IEnumerator SyncProgressionAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);
        while (isBusy)
        {
            yield return null;
        }

        SyncProgressionRequest request = new SyncProgressionRequest
        {
            attackLevel = progression.AttackLevel,
            attackSpeedLevel = progression.SpeedLevel,
            criticalLevel = progression.CriticalLevel,
            prestigeLevel = progression.PrestigeLevel,
            soulStones = progression.SoulStones
            ,equipmentTier = progression.EquipmentTier
            ,equipmentCount = progression.EquipmentCount
            ,unlockedRegion = progression.UnlockedRegion
            ,skillOneLevel = progression.SkillOneLevel
            ,skillTwoLevel = progression.SkillTwoLevel
            ,skillThreeLevel = progression.SkillThreeLevel
        };

        yield return RunRequest(api.SyncProgression(apiBaseUrl, request, result =>
        {
            if (HandleFailure(result)) return;
            syncedAttackLevel = result.response.attackLevel;
            syncedSpeedLevel = result.response.attackSpeedLevel;
            syncedCriticalLevel = result.response.criticalLevel;
            syncedPrestigeLevel = result.response.prestigeLevel;
            syncedSoulStones = result.response.soulStones;
            syncedEquipmentTier = result.response.equipmentTier;
            syncedEquipmentCount = result.response.equipmentCount;
            syncedUnlockedRegion = result.response.unlockedRegion;
            syncedSkillOneLevel = result.response.skillOneLevel;
            syncedSkillTwoLevel = result.response.skillTwoLevel;
            syncedSkillThreeLevel = result.response.skillThreeLevel;
        }));

        progressionSyncRoutine = null;
        if (!ProgressionMatchesLastSync())
        {
            OnProgressionChanged();
        }
    }

    private bool ProgressionMatchesLastSync()
    {
        return progression.AttackLevel == syncedAttackLevel &&
               progression.SpeedLevel == syncedSpeedLevel &&
               progression.CriticalLevel == syncedCriticalLevel &&
               progression.PrestigeLevel == syncedPrestigeLevel &&
               progression.SoulStones == syncedSoulStones &&
               progression.EquipmentTier == syncedEquipmentTier &&
               progression.EquipmentCount == syncedEquipmentCount &&
               progression.UnlockedRegion == syncedUnlockedRegion &&
               progression.SkillOneLevel == syncedSkillOneLevel &&
               progression.SkillTwoLevel == syncedSkillTwoLevel &&
               progression.SkillThreeLevel == syncedSkillThreeLevel;
    }

    private void RememberSyncedProgression()
    {
        syncedAttackLevel = progression.AttackLevel;
        syncedSpeedLevel = progression.SpeedLevel;
        syncedCriticalLevel = progression.CriticalLevel;
        syncedPrestigeLevel = progression.PrestigeLevel;
        syncedSoulStones = progression.SoulStones;
        syncedEquipmentTier = progression.EquipmentTier;
        syncedEquipmentCount = progression.EquipmentCount;
        syncedUnlockedRegion = progression.UnlockedRegion;
        syncedSkillOneLevel = progression.SkillOneLevel;
        syncedSkillTwoLevel = progression.SkillTwoLevel;
        syncedSkillThreeLevel = progression.SkillThreeLevel;
    }

    private void OnAutoBossDefeated(int clearedStage)
    {
        if (!session.HasToken || isBusy)
        {
            return;
        }

        StartCoroutine(SyncAutoStage(clearedStage));
    }

    private IEnumerator SyncAutoStage(int stage)
    {
        yield return RunRequest(
            api.ChallengeStage(apiBaseUrl, stage, CreateIdempotencyKey("auto-stage-" + stage), result =>
            {
                if (HandleFailure(result)) return;
                AddLog("Auto stage synced: " + stage + " / " + result.response.outcome);
            }));

        if (lastRequestSucceeded)
        {
            yield return GetGameState();
        }
    }

    private void OnApplicationQuit()
    {
        progression?.MarkExitTime();
    }

    private void OnDestroy()
    {
        if (progression != null)
        {
            progression.Changed -= OnProgressionChanged;
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            progression?.MarkExitTime();
        }
        else if (session.HasToken && !isBusy)
        {
            StartCoroutine(RestoreSessionWithRetry());
        }
    }

    private void SelectCharacter(CharacterVisualSet selectedSet)
    {
        if (characterVisualSet == selectedSet)
        {
            return;
        }

        PlayerPrefs.SetInt(CharacterVisualSetKey, (int)selectedSet);
        // 다음 Scene 초기화가 영웅 변경 때문임을 표시합니다. Awake에서 읽은 즉시 삭제되는 일회성 값입니다.
        PlayerPrefs.SetInt(SkipOfflineRewardOnceKey, 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 상태 변경 API마다 고유한 Idempotency-Key 값을 만듭니다.
    private static string CreateIdempotencyKey(string keyPrefix)
    {
        // prefix는 사람이 로그에서 의미를 알아보기 위한 값이고 GUID는 충돌 방지용입니다.
        return keyPrefix + "-" + Guid.NewGuid().ToString("N");
    }

    // 현재 세션, 게임 상태, busy 상태를 런타임 UI에 반영합니다.
    private void RefreshUi()
    {
        // Awake 초기에 UI가 아직 생성되지 않은 경우를 방어합니다.
        if (ui == null)
        {
            return;
        }

        // UI 객체는 표시만 담당하고 실제 상태 소유권은 Bootstrap이 유지합니다.
        ui.Refresh(apiBaseUrl, session, gameState, huntGold, log, isBusy, isDemoRunning);
    }
}
