using System.Collections;
using UnityEngine;

public sealed class IdleGuildGameWorld
{
    // Sprite Sheet는 가로 4프레임, 세로 4개 애니메이션 행으로 구성됩니다.
    private const int SheetColumns = 4;
    private const int SheetRows = 4;
    // Animator Controller와 Editor 빌더가 공통으로 사용하는 int 파라미터 이름입니다.
    private static readonly int AnimationStateParameter = Animator.StringToHash("State");

    // Sprite Sheet 행과 Animator 상태 값을 같은 순서로 유지하는 애니메이션 상태입니다.
    private enum ActorAnimationState
    {
        Idle = 0,
        Run = 1,
        Attack = 2,
        Hit = 3
    }

    private readonly Transform parent;
    private readonly MonoBehaviour coroutineHost;
    private readonly bool useAlternateCharacters;
    private readonly bool useBlackCatHero;
    private readonly IdleGuildBattleSceneLayout sceneLayout;
    private readonly IdleGuildProgression progression;
    private readonly IdleGuildIdleHud idleHud;

    private Transform hero;
    private Transform monster;
    private SpriteRenderer heroRenderer;
    private SpriteRenderer monsterRenderer;
    private Animator heroAnimator;
    private Animator monsterAnimator;
    private IdleGuildWorldHealthBar heroHealthBar;
    private IdleGuildWorldHealthBar monsterHealthBar;
    private TextMesh stageText;
    private TextMesh resultText;
    private Sprite hudPixelSprite;
    // Resources 폴더의 PNG에서 잘라낸 영웅/몬스터 애니메이션 프레임입니다.
    private Sprite[] heroFrames;
    private Sprite[] monsterFrames;
    private Sprite[] regionMonsterSprites;
    private Sprite[] skillEffectSprites;
    private Vector3 heroHome;
    private Vector3 monsterHome;
    private Coroutine combatRoutine;
    private Coroutine autoHuntRoutine;
    private Coroutine heroAnimationRoutine;
    private Coroutine monsterAnimationRoutine;
    private System.Action<int> onHuntGoldEarned;
    private System.Action<int> onBossDefeated;
    private readonly Transform[] roadSegments = new Transform[2];
    private float roadWidth;
    private Transform backdropTransform;
    private Vector3 backdropOrigin;
    private SpriteRenderer backdropRenderer;
    private int pendingSkillDamage;
    private int pendingSkillType;
    private bool skillAnimationPlaying;

    public IdleGuildGameWorld(
        Transform parent,
        MonoBehaviour coroutineHost,
        bool useAlternateCharacters,
        bool useBlackCatHero,
        IdleGuildBattleSceneLayout sceneLayout,
        IdleGuildProgression progression,
        IdleGuildIdleHud idleHud)
    {
        this.parent = parent;
        this.coroutineHost = coroutineHost;
        this.useAlternateCharacters = useAlternateCharacters;
        this.useBlackCatHero = useBlackCatHero;
        this.sceneLayout = sceneLayout;
        this.progression = progression;
        this.idleHud = idleHud;
    }

    public void Build()
    {
        // 실제 PNG Sprite Sheet를 먼저 불러옵니다. 실패하면 CreateActors에서 임시 Sprite를 사용합니다.
        string heroSheet = useBlackCatHero
            ? "Sprites/black-cat-red-ribbon-spritesheet"
            : useAlternateCharacters ? "Sprites/girl-hero-spritesheet-v5" : "Sprites/hero-spritesheet";
        string monsterSheet = useAlternateCharacters
            ? "Sprites/masked-thief-spritesheet"
            : "Sprites/slime-spritesheet";
        string heroCharacterName = useBlackCatHero ? "blackCat" : useAlternateCharacters ? "girlHero" : "hero";
        heroFrames = LoadSpriteSheet(heroSheet, heroCharacterName);
        monsterFrames = LoadSpriteSheet(monsterSheet, useAlternateCharacters ? "maskedThief" : "slime");
        regionMonsterSprites = LoadRegionMonsterSprites();
        skillEffectSprites = new[]
        {
            Resources.Load<Sprite>("Effects/star-burst-vfx"),
            Resources.Load<Sprite>("Effects/swift-strike-vfx"),
            Resources.Load<Sprite>("Effects/guardian-light-vfx")
        };
        CreateCameraBackdrop();
        CreateGround();
        CreateActors();
        CreateBattleHud();
        StartIdleAnimations();
        coroutineHost.StartCoroutine(ScrollRoad());
    }

    public void PlayStageChallenge(int stage, string outcome)
    {
        StopAutoHunt();
        if (combatRoutine != null)
        {
            coroutineHost.StopCoroutine(combatRoutine);
        }

        combatRoutine = coroutineHost.StartCoroutine(PlayCombat(stage, outcome));
    }

    public void StartAutoHunt(System.Action<int> goldEarned, System.Action<int> bossDefeated = null)
    {
        onHuntGoldEarned = goldEarned;
        if (bossDefeated != null)
        {
            onBossDefeated = bossDefeated;
        }
        if (autoHuntRoutine != null || combatRoutine != null)
        {
            return;
        }

        autoHuntRoutine = coroutineHost.StartCoroutine(PlayAutoHunt());
    }

    public void ActivateSkill(int skillType)
    {
        int selectedSkill = Mathf.Clamp(skillType, 0, 2);
        int[] multipliers = progression.Balance.skillDamageMultipliers;
        float levelBonus = 1f + (progression.GetSkillLevel(selectedSkill) - 1) * 0.18f;
        float characterBonus = useBlackCatHero && selectedSkill == 1 ? 1.35f :
            useAlternateCharacters && selectedSkill == 0 ? 1.25f :
            !useAlternateCharacters && selectedSkill == 2 ? 1.3f : 1f;
        int skillDamage = Mathf.RoundToInt(progression.AttackDamage * multipliers[selectedSkill] * levelBonus * characterBonus);
        coroutineHost.StartCoroutine(PlaySkillAnimation(selectedSkill, skillDamage));
    }

    private void StopAutoHunt()
    {
        if (autoHuntRoutine == null)
        {
            return;
        }

        coroutineHost.StopCoroutine(autoHuntRoutine);
        autoHuntRoutine = null;
        StopActorAnimations();
    }

    private IEnumerator PlayAutoHunt()
    {
        stageText.text = "AUTO HUNT";
        resultText.gameObject.SetActive(false);
        hero.position = heroHome;
        int encounter = 0;
        StartHeroLoop(ActorAnimationState.Run, 0.075f);

        while (true)
        {
            encounter++;
            bool boss = encounter % 7 == 0;
            idleHud.SetEncounterProgress(boss ? 7 : encounter % 7, boss);
            int regionIndex = ((progression.Stage - 1) / Mathf.Max(1, progression.Balance.stagesPerRegion)) % 3;
            string[] regionNames = { "FOREST", "CAVE", "SNOWFIELD" };
            if (backdropRenderer != null)
            {
                Color[] regionColors =
                {
                    new Color(0.9f, 1f, 0.9f, 1f),
                    new Color(0.58f, 0.52f, 0.72f, 1f),
                    new Color(0.78f, 0.9f, 1f, 1f)
                };
                backdropRenderer.color = regionColors[regionIndex];
            }
            int maxHealth = boss
                ? progression.AttackDamage * progression.Balance.bossHealthInAttacks + progression.Stage * progression.Balance.bossHealthPerStage
                : progression.AttackDamage * progression.Balance.regularMonsterHealthInAttacks;
            int health = maxHealth;
            int heroMaxHealth = Mathf.Max(30, progression.AttackDamage * 8 + progression.Stage * 6);
            int heroHealth = heroMaxHealth;
            bool heroDefeated = false;
            float bossDeadline = Time.time + progression.Balance.bossTimeLimitSeconds;
            float enemyAttackInterval = boss
                ? progression.Balance.bossAttackIntervalSeconds
                : Mathf.Max(1.4f, progression.Balance.bossAttackIntervalSeconds * 0.65f);
            float nextEnemyAttack = Time.time + (boss ? 0.9f : 0.55f);
            stageText.text = (boss ? "BOSS " : "") + regionNames[regionIndex] + " " + progression.Stage;
            monster.gameObject.SetActive(true);
            monster.localScale = boss ? new Vector3(3.15f, 3.15f, 1f) : new Vector3(2.15f, 2.15f, 1f);
            monsterRenderer.color = Color.white;
            if (regionMonsterSprites != null && regionMonsterSprites.Length == 4)
            {
                monsterAnimator.enabled = false;
                monsterRenderer.sprite = regionMonsterSprites[boss ? 3 : regionIndex];
            }
            monster.position = monsterHome + new Vector3(0.7f, 0f, 0f);
            monsterRenderer.color = new Color(1f, 1f, 1f, 0f);
            monsterHealthBar.SetHealth(health, maxHealth);
            heroHealthBar.SetHealth(heroHealth, heroMaxHealth);
            idleHud.SetBoss(true, health, maxHealth, boss ? progression.Balance.bossTimeLimitSeconds : -1f);

            if (monsterAnimator.enabled)
            {
                StartMonsterLoop(ActorAnimationState.Run, 0.11f);
            }
            yield return MoveMonsterIntoBattle(
                new Vector3(heroHome.x + 1.25f, monsterHome.y, monsterHome.z),
                0.72f,
                0.16f);

            StopMonsterAnimation();
            while (health > 0)
            {
                if (boss && Time.time >= bossDeadline)
                {
                    idleHud.ShowToast("보스 제한시간 초과!");
                    break;
                }

                if (pendingSkillDamage > 0)
                {
                    int skillDamage = pendingSkillDamage;
                    int skillType = pendingSkillType;
                    pendingSkillDamage = 0;
                    health = Mathf.Max(0, health - skillDamage);
                    monsterHealthBar.SetHealth(health, maxHealth);
                    Color[] skillDamageColors =
                    {
                        new Color(0.3f, 0.95f, 1f, 1f),
                        new Color(1f, 0.78f, 0.12f, 1f),
                        new Color(0.45f, 1f, 0.5f, 1f)
                    };
                    Color damageColor = skillDamageColors[Mathf.Clamp(skillType, 0, skillDamageColors.Length - 1)];
                    if (monsterAnimator.enabled)
                    {
                        StartActorState(monsterRenderer, monsterAnimator, monsterFrames, ActorAnimationState.Hit, 0.055f);
                    }
                    coroutineHost.StartCoroutine(PlayHitEffect(monster.position + new Vector3(0f, 0.5f, 0f)));
                    coroutineHost.StartCoroutine(ShowDamagePopup(monster.position + Vector3.up, skillDamage, damageColor));
                    coroutineHost.StartCoroutine(ShakeCamera(skillType == 1 ? 0.16f : 0.11f));
                    idleHud.SetBoss(true, health, maxHealth, boss ? Mathf.Max(0f, bossDeadline - Time.time) : -1f);
                    if (health <= 0) break;
                }

                // 스킬 연출 중에는 일반 공격과 적 공격이 캐릭터 모션을 덮어쓰지 않게 기다립니다.
                if (skillAnimationPlaying)
                {
                    yield return null;
                    continue;
                }

                if (Time.time >= nextEnemyAttack)
                {
                    int enemyDamage = Mathf.Max(1, progression.Stage * (boss ? 3 : 2));
                    yield return EnemyAttackPattern(boss, enemyDamage);
                    heroHealth = Mathf.Max(0, heroHealth - enemyDamage);
                    heroHealthBar.SetHealth(heroHealth, heroMaxHealth);
                    if (heroHealth <= 0)
                    {
                        heroDefeated = true;
                        break;
                    }
                    nextEnemyAttack = Time.time + enemyAttackInterval;
                }

                StopHeroAnimation();
                StartActorState(heroRenderer, heroAnimator, heroFrames, ActorAnimationState.Attack, 0.065f);
                // 4프레임 공격 애니메이션이 눈에 보이기 전에 Run으로 덮이지 않도록 충분히 재생합니다.
                yield return new WaitForSeconds(0.28f);

                bool critical;
                int damage = progression.RollDamage(out critical);
                IdleGuildReleaseServices.PlayEffect(critical ? 760f : 520f);
                health = Mathf.Max(0, health - damage);
                monsterHealthBar.SetHealth(health, maxHealth);
                if (monsterAnimator.enabled)
                {
                    StartActorState(monsterRenderer, monsterAnimator, monsterFrames, ActorAnimationState.Hit, 0.065f);
                }
                coroutineHost.StartCoroutine(PlayHitEffect(monster.position + new Vector3(0f, 0.45f, 0f)));
                coroutineHost.StartCoroutine(ShowDamagePopup(
                    monster.position + new Vector3(0f, 0.7f, 0f),
                    damage,
                    critical ? new Color(1f, 0.35f, 0.12f, 1f) : new Color(1f, 0.9f, 0.25f, 1f)));
                coroutineHost.StartCoroutine(ShakeCamera(critical ? 0.11f : 0.07f));
                idleHud.SetBoss(
                    true,
                    health,
                    maxHealth,
                    boss ? Mathf.Max(0f, bossDeadline - Time.time) : -1f);

                StartHeroLoop(ActorAnimationState.Run, 0.075f);
                float attackDelay = Mathf.Max(0.08f, 1f / progression.AttacksPerSecond - 0.28f);
                yield return new WaitForSeconds(attackDelay);
            }

            idleHud.SetBoss(false, 0, 1, 0f);
            if (heroDefeated)
            {
                idleHud.ShowToast("주인공 패배 - 잠시 후 다시 도전합니다");
                yield return PlayActorOnce(heroAnimator, heroRenderer, heroFrames, ActorAnimationState.Hit, 0.08f);
                yield return BumpBack(hero);
                yield return RetreatMonster();
                hero.position = heroHome;
                heroHealthBar.SetHealth(heroMaxHealth, heroMaxHealth);
                StartHeroLoop(ActorAnimationState.Run, 0.075f);
                yield return new WaitForSeconds(0.65f);
                continue;
            }
            if (health > 0)
            {
                if (boss)
                {
                    // 실패하면 한 번 일반 사냥으로 돌아간 뒤 같은 보스를 자동 재도전합니다.
                    encounter = 5;
                    idleHud.ShowToast("보스 실패 - 일반 사냥 후 재도전");
                }
                yield return KnockOutMonster();
                yield return new WaitForSeconds(0.3f);
                continue;
            }

            int reward = progression.RewardFor(boss);
            if (boss)
            {
                onBossDefeated?.Invoke(Mathf.Max(1, progression.Stage - 1));
                idleHud.ShowToast("BOSS CHEST OPEN!");
            }
            onHuntGoldEarned?.Invoke(reward);
            idleHud.ShowGold(reward);
            if (progression.TryDropAndAutoEquip(boss))
            {
                idleHud.ShowToast("장비 획득! Tier " + progression.EquipmentTier + " 자동 장착");
            }
            yield return KnockOutMonster();
            yield return new WaitForSeconds(boss ? 0.35f : 0.04f);
        }
    }

    private IEnumerator PlayCombat(int stage, string outcome)
    {
        // 서버 결과를 HUD와 전투 연출에서 사용할 클라이언트 전용 표시 데이터로 변환합니다.
        IdleGuildBattlePresentation presentation = IdleGuildBattlePresentation.Create(stage, outcome);
        // 이전 Idle 애니메이션을 멈추고 전투 시작 상태로 되돌립니다.
        StopActorAnimations();
        hero.position = heroHome;
        monster.position = monsterHome;
        monsterAnimator.enabled = true;
        monster.localScale = new Vector3(3f, 3f, 1f);
        heroRenderer.flipX = false;
        monster.gameObject.SetActive(true);
        monsterRenderer.color = Color.white;
        PrepareBattleHud(presentation);
        StartMonsterLoop(ActorAnimationState.Idle, 0.16f);

        // Run 행을 반복 재생하면서 영웅의 Transform을 몬스터 앞으로 이동시킵니다.
        Vector3 attackPoint = new Vector3(monsterHome.x - 1.1f, heroHome.y, monsterHome.z);
        StartHeroLoop(ActorAnimationState.Run, 0.09f);
        yield return MoveTo(hero, attackPoint, 0.55f);
        StopHeroAnimation();

        // Animator의 Attack 상태를 한 번 재생하고 클립 길이만큼 기다립니다.
        yield return PlayActorOnce(
            heroAnimator,
            heroRenderer,
            heroFrames,
            ActorAnimationState.Attack,
            0.10f);

        // 공격 결과를 체력 바, 데미지 숫자, 타격 플래시에 동시에 반영합니다.
        coroutineHost.StartCoroutine(ShowDamagePopup(
            monster.position + new Vector3(0f, 1.4f, 0f),
            presentation.DamageToMonster,
            new Color(1f, 0.82f, 0.2f, 1f)));
        coroutineHost.StartCoroutine(PlayHitEffect(monster.position + new Vector3(0f, 0.8f, 0f)));
        yield return AnimateHealth(
            monsterHealthBar,
            presentation.MonsterMaxHealth,
            presentation.MonsterHealthAfter,
            presentation.MonsterMaxHealth,
            0.28f);

        StopMonsterAnimation();
        yield return PlayActorOnce(
            monsterAnimator,
            monsterRenderer,
            monsterFrames,
            ActorAnimationState.Hit,
            0.10f);

        if (presentation.IsVictory)
        {
            // 승리하면 체력이 0이 된 슬라임이 밀려나며 사라집니다.
            yield return KnockOutMonster();
        }
        else
        {
            // 실패하면 살아남은 슬라임이 반격하고 영웅 체력과 피격 연출을 갱신합니다.
            yield return PlayActorOnce(
                monsterAnimator,
                monsterRenderer,
                monsterFrames,
                ActorAnimationState.Attack,
                0.10f);
            coroutineHost.StartCoroutine(ShowDamagePopup(
                hero.position + new Vector3(0f, 1.5f, 0f),
                presentation.DamageToHero,
                new Color(1f, 0.36f, 0.3f, 1f)));
            coroutineHost.StartCoroutine(PlayHitEffect(hero.position + new Vector3(0f, 0.9f, 0f)));
            yield return AnimateHealth(
                heroHealthBar,
                presentation.HeroMaxHealth,
                presentation.HeroHealthAfter,
                presentation.HeroMaxHealth,
                0.28f);
            yield return PlayActorOnce(
                heroAnimator,
                heroRenderer,
                heroFrames,
                ActorAnimationState.Hit,
                0.10f);
            yield return BumpBack(hero);
        }

        ShowBattleResult(presentation);

        // 복귀할 때는 왼쪽을 보도록 flipX를 켜고 Run 행을 다시 반복합니다.
        heroRenderer.flipX = true;
        StartHeroLoop(ActorAnimationState.Run, 0.09f);
        yield return MoveTo(hero, heroHome, 0.45f);
        StopHeroAnimation();
        heroRenderer.flipX = false;

        // 전투가 끝나면 살아 있는 캐릭터를 다시 Idle 애니메이션으로 전환합니다.
        StartIdleAnimations();
        combatRoutine = null;
        StartAutoHunt(onHuntGoldEarned, onBossDefeated);
    }

    // 전투 시작 시 스테이지 제목과 양쪽 체력을 초기 상태로 되돌립니다.
    private void PrepareBattleHud(IdleGuildBattlePresentation presentation)
    {
        stageText.text = presentation.StageLabel;
        resultText.gameObject.SetActive(false);
        heroHealthBar.SetHealth(presentation.HeroMaxHealth, presentation.HeroMaxHealth);
        monsterHealthBar.SetHealth(presentation.MonsterMaxHealth, presentation.MonsterMaxHealth);
    }

    // 서버 승패 결과를 화면 중앙의 결과 텍스트로 표시합니다.
    private void ShowBattleResult(IdleGuildBattlePresentation presentation)
    {
        resultText.text = presentation.ResultLabel;
        resultText.color = presentation.IsVictory
            ? new Color(1f, 0.84f, 0.28f, 1f)
            : new Color(1f, 0.38f, 0.34f, 1f);
        resultText.gameObject.SetActive(true);
    }

    // 체력 바를 시작 HP에서 목표 HP까지 부드럽게 감소시킵니다.
    private static IEnumerator AnimateHealth(
        IdleGuildWorldHealthBar healthBar,
        int fromHealth,
        int toHealth,
        int maxHealth,
        float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            int currentHealth = Mathf.RoundToInt(Mathf.Lerp(fromHealth, toHealth, progress));
            healthBar.SetHealth(currentHealth, maxHealth);
            yield return null;
        }

        healthBar.SetHealth(toHealth, maxHealth);
    }

    // 공격 위치에서 데미지 숫자가 위로 떠오르며 사라지는 연출입니다.
    private IEnumerator ShowDamagePopup(Vector3 worldPosition, int damage, Color color)
    {
        TextMesh popup = CreateWorldText(
            "Damage Popup",
            "-" + damage,
            worldPosition,
            0.13f,
            40,
            color,
            32);

        float elapsed = 0f;
        const float duration = 0.7f;
        Vector3 start = popup.transform.position;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            popup.transform.position = start + new Vector3(0f, 0.65f * progress, 0f);
            popup.color = new Color(color.r, color.g, color.b, 1f - progress);
            yield return null;
        }

        Object.Destroy(popup.gameObject);
    }

    // 작은 사각 Sprite들이 공격 지점에서 퍼져 나가는 타격 플래시입니다.
    private IEnumerator PlayHitEffect(Vector3 worldPosition)
    {
        int particleCount = 8 + Mathf.Min(8, progression.EquipmentTier * 2);
        GameObject effectRoot = new GameObject("Hit Effect");
        effectRoot.transform.SetParent(parent, false);
        effectRoot.transform.position = worldPosition;
        Transform[] particles = new Transform[particleCount];
        SpriteRenderer[] renderers = new SpriteRenderer[particleCount];
        Vector3[] directions = new Vector3[particleCount];

        for (int index = 0; index < particleCount; index++)
        {
            float angle = index * Mathf.PI * 2f / particleCount;
            GameObject particle = new GameObject("Hit Pixel " + index);
            particle.transform.SetParent(effectRoot.transform, false);
            float effectScale = 0.16f + Mathf.Min(0.12f, progression.AttackLevel * 0.008f);
            particle.transform.localScale = new Vector3(effectScale, effectScale, 1f);
            particles[index] = particle.transform;

            SpriteRenderer renderer = particle.AddComponent<SpriteRenderer>();
            renderer.sprite = hudPixelSprite;
            renderer.color = progression.EquipmentTier >= 3
                ? (index % 2 == 0 ? new Color(0.35f, 0.9f, 1f, 1f) : new Color(0.65f, 0.35f, 1f, 1f))
                : (index % 2 == 0 ? new Color(1f, 0.9f, 0.25f, 1f) : new Color(1f, 0.45f, 0.18f, 1f));
            renderer.sortingOrder = 28;
            renderers[index] = renderer;

            // 현재 위치와 분리된 단위 방향을 저장해 중심에서 바깥쪽으로만 이동시킵니다.
            directions[index] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            particle.transform.localPosition = directions[index] * 0.12f;
        }

        float elapsed = 0f;
        const float duration = 0.34f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            for (int index = 0; index < particleCount; index++)
            {
                particles[index].localPosition = directions[index] * (0.12f + 0.58f * progress);
                Color particleColor = renderers[index].color;
                particleColor.a = 1f - progress;
                renderers[index].color = particleColor;
            }

            yield return null;
        }

        Object.Destroy(effectRoot);
    }

    // 지정한 Sprite Sheet 행의 네 프레임을 순서대로 재생합니다.
    private static IEnumerator PlayAnimation(
        SpriteRenderer renderer,
        Sprite[] frames,
        ActorAnimationState state,
        float frameDuration,
        bool loop)
    {
        // 이미지 로드 실패 시 fallback Sprite를 유지하고 한 프레임만 기다립니다.
        if (frames == null || frames.Length != SheetColumns * SheetRows)
        {
            yield return null;
            yield break;
        }

        int firstFrame = (int)state * SheetColumns;
        do
        {
            for (int column = 0; column < SheetColumns; column++)
            {
                renderer.sprite = frames[firstFrame + column];
                yield return new WaitForSeconds(frameDuration);
            }
        }
        while (loop);
    }

    // Animator 애셋이 있으면 Controller 상태를, 없으면 Coroutine 프레임을 한 번 재생합니다.
    private static IEnumerator PlayActorOnce(
        Animator animator,
        SpriteRenderer renderer,
        Sprite[] frames,
        ActorAnimationState state,
        float fallbackFrameDuration)
    {
        if (HasAnimatorController(animator))
        {
            SetAnimatorState(animator, state);
            yield return new WaitForSeconds(fallbackFrameDuration * SheetColumns);
            yield break;
        }

        yield return PlayAnimation(renderer, frames, state, fallbackFrameDuration, false);
    }

    // 영웅과 활성 상태인 몬스터의 대기 동작을 시작합니다.
    private void StartIdleAnimations()
    {
        StopActorAnimations();
        StartHeroLoop(ActorAnimationState.Idle, 0.18f);

        if (monster.gameObject.activeSelf)
        {
            StartMonsterLoop(ActorAnimationState.Idle, 0.18f);
        }
    }

    // 영웅의 반복 상태를 Animator 또는 fallback Coroutine으로 시작합니다.
    private void StartHeroLoop(ActorAnimationState state, float fallbackFrameDuration)
    {
        StopHeroAnimation();
        if (HasAnimatorController(heroAnimator))
        {
            SetAnimatorState(heroAnimator, state);
            return;
        }

        heroAnimationRoutine = coroutineHost.StartCoroutine(
            PlayAnimation(heroRenderer, heroFrames, state, fallbackFrameDuration, true));
    }

    // 몬스터의 반복 상태를 Animator 또는 fallback Coroutine으로 시작합니다.
    private void StartMonsterLoop(ActorAnimationState state, float fallbackFrameDuration)
    {
        StopMonsterAnimation();
        if (HasAnimatorController(monsterAnimator))
        {
            SetAnimatorState(monsterAnimator, state);
            return;
        }

        monsterAnimationRoutine = coroutineHost.StartCoroutine(
            PlayAnimation(monsterRenderer, monsterFrames, state, fallbackFrameDuration, true));
    }

    private static bool HasAnimatorController(Animator animator)
    {
        return animator != null && animator.runtimeAnimatorController != null;
    }

    private static void SetAnimatorState(Animator animator, ActorAnimationState state)
    {
        animator.SetInteger(AnimationStateParameter, (int)state);
    }

    private void StartActorState(
        SpriteRenderer renderer,
        Animator animator,
        Sprite[] frames,
        ActorAnimationState state,
        float frameDuration)
    {
        if (HasAnimatorController(animator))
        {
            SetAnimatorState(animator, state);
            return;
        }

        coroutineHost.StartCoroutine(PlayAnimation(renderer, frames, state, frameDuration, false));
    }

    // 현재 캐릭터 애니메이션 코루틴을 모두 안전하게 중지합니다.
    private void StopActorAnimations()
    {
        StopHeroAnimation();
        StopMonsterAnimation();
    }

    private void StopHeroAnimation()
    {
        if (heroAnimationRoutine == null)
        {
            return;
        }

        coroutineHost.StopCoroutine(heroAnimationRoutine);
        heroAnimationRoutine = null;
    }

    private void StopMonsterAnimation()
    {
        if (monsterAnimationRoutine == null)
        {
            return;
        }

        coroutineHost.StopCoroutine(monsterAnimationRoutine);
        monsterAnimationRoutine = null;
    }

    private IEnumerator MoveTo(Transform target, Vector3 destination, float duration)
    {
        Vector3 origin = target.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            target.position = Vector3.Lerp(origin, destination, Smooth(t));
            yield return null;
        }

        target.position = destination;
    }

    private IEnumerator MoveMonsterIntoBattle(Vector3 destination, float duration, float fadeDuration)
    {
        Vector3 origin = monster.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            monster.position = Vector3.Lerp(origin, destination, Mathf.Clamp01(elapsed / duration));
            float alpha = Mathf.Clamp01(elapsed / fadeDuration);
            monsterRenderer.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        monster.position = destination;
        monsterRenderer.color = Color.white;
    }

    private IEnumerator KnockOutMonster()
    {
        Vector3 origin = monster.position;
        float elapsed = 0f;
        while (elapsed < 0.24f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / 0.24f);
            monster.position = origin + new Vector3(0.45f * t, 0.35f * t, 0f);
            monsterRenderer.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }

        monster.gameObject.SetActive(false);
    }

    private IEnumerator ShakeCamera(float strength)
    {
        Camera targetCamera = Camera.main;
        if (targetCamera == null)
        {
            yield break;
        }

        Transform cameraTransform = targetCamera.transform;
        Vector3 origin = cameraTransform.position;
        float elapsed = 0f;
        const float duration = 0.12f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float fade = 1f - Mathf.Clamp01(elapsed / duration);
            cameraTransform.position = origin + (Vector3)(UnityEngine.Random.insideUnitCircle * strength * fade);
            yield return null;
        }

        cameraTransform.position = origin;
    }

    private IEnumerator PlaySkillAnimation(int skillType, int skillDamage)
    {
        while (skillAnimationPlaying)
        {
            yield return null;
        }

        skillAnimationPlaying = true;
        StopHeroAnimation();
        Vector3 originalPosition = hero.position;
        Vector3 originalScale = hero.localScale;
        Color originalColor = heroRenderer.color;
        StartActorState(heroRenderer, heroAnimator, heroFrames, ActorAnimationState.Attack, 0.06f);

        if (skillType == 0)
        {
            yield return PlayStarBurst(originalPosition, originalScale, skillDamage);
        }
        else if (skillType == 1)
        {
            yield return PlaySwiftStrike(originalPosition, skillDamage);
        }
        else
        {
            yield return PlayGuardianLight(originalPosition, originalScale, skillDamage);
        }

        hero.position = originalPosition;
        hero.localScale = originalScale;
        heroRenderer.color = originalColor;
        skillAnimationPlaying = false;
        StartHeroLoop(autoHuntRoutine != null ? ActorAnimationState.Run : ActorAnimationState.Idle, 0.075f);
    }

    private IEnumerator PlayStarBurst(Vector3 origin, Vector3 originalScale, int skillDamage)
    {
        IdleGuildReleaseServices.PlayEffect(880f);
        Vector3 impactPosition = monster.gameObject.activeSelf
            ? monster.position + Vector3.up * 0.45f
            : origin + Vector3.right * 2.4f;
        ShowLargeSkillEffect(0, impactPosition, 2.7f, 0.52f, 80f);
        const int count = 18;
        for (int index = 0; index < count; index++)
        {
            float angle = index * Mathf.PI * 2f / count;
            Vector3 start = origin + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.72f;
            CreateSkillParticle("Star Burst", start, 0.13f,
                index % 2 == 0 ? new Color(0.3f, 0.95f, 1f, 1f) : Color.white,
                monster.gameObject.activeSelf ? monster.position + Vector3.up * 0.45f : start + Vector3.right * 3f,
                0.38f);
        }

        yield return MoveTo(hero, origin + Vector3.up * 0.18f, 0.12f);
        hero.localScale = originalScale * 1.12f;
        QueueSkillImpact(0, skillDamage);
        yield return ShakeCamera(0.16f);
        yield return MoveTo(hero, origin, 0.14f);
    }

    private IEnumerator PlaySwiftStrike(Vector3 origin, int skillDamage)
    {
        IdleGuildReleaseServices.PlayEffect(1060f);
        Vector3 destination = monster.gameObject.activeSelf
            ? new Vector3(monster.position.x - 0.75f, origin.y, origin.z)
            : origin + Vector3.right * 2.2f;
        for (int index = 0; index < 4; index++)
        {
            CreateSkillParticle("Swift Afterimage", origin + Vector3.right * index * 0.28f, 0.32f,
                new Color(1f, 0.8f, 0.2f, 0.7f), destination, 0.22f + index * 0.025f);
        }

        yield return MoveTo(hero, destination, 0.13f);
        ShowLargeSkillEffect(1, destination + new Vector3(0.65f, 0.45f, 0f), 3f, 0.44f, -35f);
        QueueSkillImpact(1, skillDamage);
        coroutineHost.StartCoroutine(ShakeCamera(0.12f));
        yield return new WaitForSeconds(0.08f);
        yield return MoveTo(hero, origin, 0.17f);
    }

    private IEnumerator PlayGuardianLight(Vector3 origin, Vector3 originalScale, int skillDamage)
    {
        IdleGuildReleaseServices.PlayEffect(640f);
        ShowLargeSkillEffect(2, origin + Vector3.up * 0.55f, 3.3f, 0.62f, 22f);
        const int count = 16;
        for (int index = 0; index < count; index++)
        {
            float angle = index * Mathf.PI * 2f / count;
            Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            Vector3 start = origin + direction * 0.28f;
            CreateSkillParticle("Guardian Aura", start, 0.16f,
                index % 2 == 0 ? new Color(0.4f, 1f, 0.55f, 0.9f) : new Color(1f, 0.95f, 0.45f, 0.9f),
                origin + direction * 1.15f, 0.48f);
        }

        yield return new WaitForSeconds(0.12f);
        QueueSkillImpact(2, skillDamage);

        float elapsed = 0f;
        const float duration = 0.48f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pulse = 1f + Mathf.Sin(elapsed * 28f) * 0.07f;
            hero.localScale = originalScale * pulse;
            heroRenderer.color = Color.Lerp(Color.white, new Color(0.55f, 1f, 0.65f, 1f), Mathf.PingPong(elapsed * 4f, 0.45f));
            yield return null;
        }
    }

    private void QueueSkillImpact(int skillType, int damage)
    {
        pendingSkillType = Mathf.Clamp(skillType, 0, 2);
        pendingSkillDamage += Mathf.Max(1, damage);
    }

    private void CreateSkillParticle(string objectName, Vector3 start, float scale, Color color, Vector3 destination, float duration)
    {
        GameObject particle = new GameObject(objectName);
        particle.transform.SetParent(parent, false);
        particle.transform.position = start;
        particle.transform.localScale = Vector3.one * scale;
        SpriteRenderer renderer = particle.AddComponent<SpriteRenderer>();
        renderer.sprite = hudPixelSprite;
        renderer.color = color;
        renderer.sortingOrder = 35;
        coroutineHost.StartCoroutine(FlySkillParticle(particle.transform, renderer, destination, duration));
    }

    private void ShowLargeSkillEffect(int skillIndex, Vector3 position, float worldSize, float duration, float rotationSpeed)
    {
        if (skillEffectSprites == null || skillIndex < 0 || skillIndex >= skillEffectSprites.Length || skillEffectSprites[skillIndex] == null)
        {
            return;
        }

        GameObject effect = new GameObject("Large Skill VFX " + skillIndex);
        effect.transform.SetParent(parent, false);
        effect.transform.position = position;
        SpriteRenderer renderer = effect.AddComponent<SpriteRenderer>();
        renderer.sprite = skillEffectSprites[skillIndex];
        renderer.sortingOrder = 38;
        renderer.color = new Color(1f, 1f, 1f, 0f);
        float spriteSize = Mathf.Max(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y);
        float targetScale = spriteSize <= 0f ? 1f : worldSize / spriteSize;
        coroutineHost.StartCoroutine(AnimateLargeSkillEffect(effect.transform, renderer, targetScale, duration, rotationSpeed));
    }

    private IEnumerator AnimateLargeSkillEffect(
        Transform effect,
        SpriteRenderer renderer,
        float targetScale,
        float duration,
        float rotationSpeed)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float appear = Mathf.Clamp01(t / 0.18f);
            float disappear = 1f - Mathf.Clamp01((t - 0.62f) / 0.38f);
            float pulse = 0.72f + Mathf.Sin(t * Mathf.PI) * 0.38f;
            effect.localScale = Vector3.one * targetScale * pulse;
            effect.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
            renderer.color = new Color(1f, 1f, 1f, appear * disappear);
            yield return null;
        }

        Object.Destroy(effect.gameObject);
    }

    private IEnumerator FlySkillParticle(Transform particle, SpriteRenderer renderer, Vector3 destination, float duration)
    {
        Vector3 start = particle.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            particle.position = Vector3.Lerp(start, destination, Smooth(t));
            particle.localScale = Vector3.one * Mathf.Lerp(particle.localScale.x, 0.04f, t);
            Color color = renderer.color;
            color.a = 1f - t;
            renderer.color = color;
            yield return null;
        }

        Object.Destroy(particle.gameObject);
    }

    private IEnumerator EnemyAttackPattern(bool boss, int damage)
    {
        if (boss) idleHud.ShowToast("BOSS ATTACK!");
        if (monsterAnimator.enabled)
        {
            StartActorState(monsterRenderer, monsterAnimator, monsterFrames, ActorAnimationState.Attack, 0.07f);
        }
        Vector3 origin = monster.position;
        yield return MoveTo(monster, origin + Vector3.left * (boss ? 0.52f : 0.32f), 0.14f);
        StartActorState(heroRenderer, heroAnimator, heroFrames, ActorAnimationState.Hit, 0.07f);
        IdleGuildReleaseServices.PlayEffect(boss ? 185f : 235f);
        coroutineHost.StartCoroutine(ShakeCamera(boss ? 0.13f : 0.07f));
        coroutineHost.StartCoroutine(ShowDamagePopup(hero.position + Vector3.up * 0.65f, damage, new Color(1f, 0.3f, 0.25f, 1f)));
        yield return BumpBack(hero);
        yield return MoveTo(monster, origin, 0.18f);
        if (monsterAnimator.enabled)
        {
            StartMonsterLoop(ActorAnimationState.Idle, 0.12f);
        }
    }

    private IEnumerator RetreatMonster()
    {
        Vector3 start = monster.position;
        Color startColor = monsterRenderer.color;
        float elapsed = 0f;
        const float duration = 0.35f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            monster.position = Vector3.Lerp(start, monsterHome + Vector3.right * 0.8f, t);
            monsterRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
            yield return null;
        }
        monster.gameObject.SetActive(false);
        monsterRenderer.color = Color.white;
    }

    private IEnumerator ScrollRoad()
    {
        while (true)
        {
            if (roadWidth > 0f)
            {
                float movement = 0.55f * Time.deltaTime;
                for (int index = 0; index < roadSegments.Length; index++)
                {
                    Transform segment = roadSegments[index];
                    if (segment == null) continue;
                    segment.position += Vector3.left * movement;
                    if (segment.position.x <= -roadWidth)
                    {
                        segment.position += Vector3.right * roadWidth * 2f;
                    }
                }
            }

            if (backdropTransform != null)
            {
                backdropTransform.position = backdropOrigin + new Vector3(Mathf.Sin(Time.time * 0.18f) * 0.14f, 0f, 0f);
            }

            yield return null;
        }
    }

    private IEnumerator BumpBack(Transform target)
    {
        Vector3 origin = target.position;
        Vector3 bumped = origin + new Vector3(-0.35f, 0f, 0f);
        yield return MoveTo(target, bumped, 0.12f);
        yield return MoveTo(target, origin, 0.18f);
    }

    private void CreateCameraBackdrop()
    {
        GameObject backdrop = new GameObject("Game Backdrop");
        Transform backdropParent = sceneLayout != null ? sceneLayout.BackdropAnchor : null;
        backdrop.transform.SetParent(backdropParent != null ? backdropParent : parent, false);
        backdrop.transform.position = backdropParent != null
            ? backdropParent.position
            : new Vector3(0f, 0f, 8f);
        backdropTransform = backdrop.transform;
        backdropOrigin = backdrop.transform.position;

        SpriteRenderer renderer = backdrop.AddComponent<SpriteRenderer>();
        backdropRenderer = renderer;
        Texture2D mountainTexture = Resources.Load<Texture2D>("Backgrounds/mountain-hunt");
        if (mountainTexture != null)
        {
            renderer.sprite = Sprite.Create(
                mountainTexture,
                new Rect(0f, 0f, mountainTexture.width, mountainTexture.height),
                new Vector2(0.5f, 0.5f),
                mountainTexture.height / 8f,
                0,
                SpriteMeshType.FullRect);
        }
        else
        {
            renderer.sprite = CreateSolidSprite("Backdrop Sprite", 1, 1, new Color(0.12f, 0.17f, 0.22f, 1f));
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(14f, 8f);
        }
        renderer.sortingOrder = -20;
    }

    private void CreateGround()
    {
        Transform groundParent = sceneLayout != null ? sceneLayout.GroundAnchor : null;
        Texture2D texture = Resources.Load<Texture2D>("Environment/mountain-road-strip");
        if (texture == null)
        {
            GameObject fallback = new GameObject("Mountain Road Fallback");
            fallback.transform.SetParent(groundParent != null ? groundParent : parent, false);
            fallback.transform.position = new Vector3(0f, -2.1f, 0f);
            SpriteRenderer fallbackRenderer = fallback.AddComponent<SpriteRenderer>();
            fallbackRenderer.sprite = CreateSolidSprite("Ground Sprite", 1, 1, new Color(0.28f, 0.32f, 0.22f, 1f));
            fallbackRenderer.drawMode = SpriteDrawMode.Sliced;
            fallbackRenderer.size = new Vector2(15f, 1.2f);
            fallbackRenderer.sortingOrder = -10;
            return;
        }

        Sprite roadSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            texture.height / 3.7f,
            0,
            SpriteMeshType.FullRect);
        roadWidth = roadSprite.bounds.size.x;
        Vector3 basePosition = groundParent != null ? groundParent.position : new Vector3(0f, -1.75f, 0f);
        for (int index = 0; index < roadSegments.Length; index++)
        {
            GameObject road = new GameObject("Scrolling Mountain Road " + index);
            road.transform.SetParent(groundParent != null ? groundParent : parent, false);
            road.transform.position = basePosition + Vector3.right * roadWidth * index;
            SpriteRenderer renderer = road.AddComponent<SpriteRenderer>();
            renderer.sprite = roadSprite;
            renderer.sortingOrder = -5;
            roadSegments[index] = road.transform;
        }
    }

    private void CreateActors()
    {
        // 생성된 Sprite Sheet마다 셀 아래쪽 투명 여백이 다릅니다.
        // 자동 사냥에서 가장 오래 보이는 Run 행의 발바닥 선을 기준으로 소녀와 고양이를 아래로 보정합니다.
        // Transform 값이 아니라 실제 불투명 픽셀의 바닥을 맞추기 위한 캐릭터별 시각 오프셋입니다.
        // Spawn Anchor의 Y를 실제 캐릭터 Transform Y로 그대로 사용합니다.
        // 사용자가 Scene에서 맞춘 위치가 캐릭터 종류에 따라 다시 이동하지 않도록 추가 Y 보정은 적용하지 않습니다.
        float heroVisualYOffset = 0f;
        Vector3 defaultHeroHome = new Vector3(-2.8f, -2.5f, 0f);
        float monsterBattleY = useAlternateCharacters ? -3.15f : -2.75f;
        Vector3 defaultMonsterHome = new Vector3(1.8f, monsterBattleY, 0f);
        Vector3 configuredHeroHome = sceneLayout != null && sceneLayout.HeroSpawn != null
            ? sceneLayout.HeroSpawn.position
            : defaultHeroHome;
        // 열린 Unity Scene이 외부 파일 변경 전의 -1.35 값을 메모리에 들고 있어도 요청한 Y를 강제로 사용합니다.
        heroHome = new Vector3(configuredHeroHome.x, -2.5f + heroVisualYOffset, configuredHeroHome.z);
        Vector3 configuredMonsterHome = sceneLayout != null && sceneLayout.MonsterSpawn != null
            ? sceneLayout.MonsterSpawn.position
            : defaultMonsterHome;
        // Sprite Sheet마다 셀 내부의 발바닥 위치와 Pivot이 달라 적 종류별 전투선 Y를 적용합니다.
        // Masked Thief는 -3.15, 기본 Slime은 기존 -2.75를 사용합니다.
        monsterHome = new Vector3(configuredMonsterHome.x, monsterBattleY, configuredMonsterHome.z);

        // PNG를 정상 로드했으면 첫 Idle 프레임을, 실패했으면 기존 코드 생성 Sprite를 사용합니다.
        Sprite heroSprite = heroFrames != null ? heroFrames[0] : CreateHeroSprite();
        Sprite monsterSprite = monsterFrames != null ? monsterFrames[0] : CreateMonsterSprite();
        string heroPrefabPath = useBlackCatHero
            ? "Prefabs/Characters/BlackCat"
            : useAlternateCharacters ? "Prefabs/Characters/GirlHero" : "Prefabs/Characters/ClassicHero";
        string monsterPrefabPath = useAlternateCharacters
            ? "Prefabs/Characters/MaskedThief"
            : "Prefabs/Characters/Slime";
        heroRenderer = CreateActorFromPrefabOrFallback(
            heroPrefabPath,
            useBlackCatHero ? "Black Cat Hero" : useAlternateCharacters ? "Cute Girl Hero" : "Pixel Hero",
            heroHome,
            heroSprite,
            5);
        monsterRenderer = CreateActorFromPrefabOrFallback(
            monsterPrefabPath,
            useAlternateCharacters ? "Masked Thief" : "Training Slime",
            monsterHome,
            monsterSprite,
            5);

        // Resources에 생성된 Controller를 각 캐릭터의 Animator 컴포넌트에 연결합니다.
        heroAnimator = GetOrCreateAnimator(
            heroRenderer.gameObject,
            useBlackCatHero
                ? "Animations/BlackCat/BlackCatAnimator"
                : useAlternateCharacters ? "Animations/GirlHeroV5/GirlHeroAnimator" : "Animations/Hero/HeroAnimator");
        monsterAnimator = GetOrCreateAnimator(
            monsterRenderer.gameObject,
            useAlternateCharacters ? "Animations/MaskedThief/MaskedThiefAnimator" : "Animations/Slime/SlimeAnimator");

        hero = heroRenderer.transform;
        monster = monsterRenderer.transform;
    }

    // 체력 바와 스테이지/결과 텍스트 등 전투 중 계속 사용하는 HUD를 생성합니다.
    private void CreateBattleHud()
    {
        // PPU 1을 사용해 체력 바의 Transform 크기가 월드 단위와 직접 일치하게 합니다.
        hudPixelSprite = CreateSolidSprite("HUD Pixel", 1, 1, Color.white, 1f);
        heroHealthBar = new IdleGuildWorldHealthBar(
            hero,
            hudPixelSprite,
            "Hero Health Bar",
            new Color(0.18f, 0.78f, 0.42f, 1f));
        monsterHealthBar = new IdleGuildWorldHealthBar(
            monster,
            hudPixelSprite,
            "Monster Health Bar",
            new Color(0.92f, 0.26f, 0.25f, 1f));

        stageText = CreateWorldText(
            "Stage Label",
            "STAGE READY",
            new Vector3(0f, 2.55f, 0f),
            0.12f,
            34,
            new Color(0.9f, 0.93f, 0.96f, 1f),
            24);
        resultText = CreateWorldText(
            "Battle Result",
            string.Empty,
            new Vector3(0f, 1.9f, 0f),
            0.16f,
            44,
            Color.white,
            25);
        resultText.gameObject.SetActive(false);
    }

    // 월드 공간에 정렬 순서를 가진 TextMesh를 생성합니다.
    private TextMesh CreateWorldText(
        string objectName,
        string value,
        Vector3 position,
        float characterSize,
        int fontSize,
        Color color,
        int sortingOrder)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);
        textObject.transform.position = position;

        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textMesh.font = font;
        textMesh.text = value;
        textMesh.fontSize = fontSize;
        textMesh.characterSize = characterSize;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = color;

        MeshRenderer renderer = textObject.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = font.material;
        renderer.sortingOrder = sortingOrder;
        return textMesh;
    }

    // Resources의 4x4 PNG를 위에서 아래, 왼쪽에서 오른쪽 순서의 Sprite 배열로 나눕니다.
    private static Sprite[] LoadSpriteSheet(string resourcePath, string characterName)
    {
        // Editor 빌더가 분할한 Sprite 하위 애셋이 있으면 이름 규칙대로 먼저 불러옵니다.
        Sprite[] importedFrames = LoadImportedSpriteFrames(resourcePath, characterName);
        if (importedFrames != null)
        {
            return importedFrames;
        }

        // Animator 애셋 생성 전에도 동작하도록 Texture를 런타임에 나누는 fallback을 유지합니다.
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            Debug.LogWarning("Sprite Sheet was not found in Resources: " + resourcePath);
            return null;
        }

        // Point 필터는 확대된 도트 이미지의 경계가 흐려지는 것을 막습니다.
        texture.filterMode = FilterMode.Point;
        int frameWidth = texture.width / SheetColumns;
        int frameHeight = texture.height / SheetRows;
        Sprite[] frames = new Sprite[SheetColumns * SheetRows];

        for (int row = 0; row < SheetRows; row++)
        {
            for (int column = 0; column < SheetColumns; column++)
            {
                // Texture 좌표 원점은 왼쪽 아래이므로 문서상의 위쪽 행부터 읽도록 Y를 뒤집습니다.
                int x = column * frameWidth;
                int y = texture.height - (row + 1) * frameHeight;
                Rect frameRect = new Rect(x, y, frameWidth, frameHeight);
                int index = row * SheetColumns + column;
                frames[index] = Sprite.Create(
                    texture,
                    frameRect,
                    new Vector2(0.5f, 0f),
                    frameWidth,
                    0,
                    SpriteMeshType.FullRect);
                frames[index].name = resourcePath + "-" + row + "-" + column;
            }
        }

        return frames;
    }

    private static Sprite[] LoadRegionMonsterSprites()
    {
        Texture2D texture = Resources.Load<Texture2D>("Sprites/monster-roster-regions");
        if (texture == null) return null;
        int width = texture.width / 2;
        int height = texture.height / 2;
        Sprite[] sprites = new Sprite[4];
        for (int row = 0; row < 2; row++)
        {
            for (int column = 0; column < 2; column++)
            {
                int index = row * 2 + column;
                sprites[index] = Sprite.Create(
                    texture,
                    new Rect(column * width, texture.height - (row + 1) * height, width, height),
                    new Vector2(0.5f, 0f),
                    width,
                    0,
                    SpriteMeshType.FullRect);
            }
        }

        return sprites;
    }

    // Multiple Sprite Import로 생성된 하위 Sprite를 상태 행/열 순서의 배열로 정렬합니다.
    private static Sprite[] LoadImportedSpriteFrames(string resourcePath, string characterName)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
        if (sprites == null || sprites.Length != SheetColumns * SheetRows)
        {
            return null;
        }

        string[] stateNames = { "idle", "run", "attack", "hit" };
        Sprite[] orderedFrames = new Sprite[SheetColumns * SheetRows];

        for (int row = 0; row < SheetRows; row++)
        {
            for (int column = 0; column < SheetColumns; column++)
            {
                string expectedName = characterName + "_" + stateNames[row] + "_" + column;
                Sprite frame = System.Array.Find(sprites, sprite => sprite.name == expectedName);
                if (frame == null)
                {
                    Debug.LogWarning("Imported Sprite frame was not found: " + expectedName);
                    return null;
                }

                orderedFrames[row * SheetColumns + column] = frame;
            }
        }

        return orderedFrames;
    }

    // 캐릭터 GameObject에 Animator를 붙이고 Resources의 Controller를 할당합니다.
    private static Animator GetOrCreateAnimator(GameObject actor, string controllerResourcePath)
    {
        Animator animator = actor.GetComponent<Animator>();
        if (animator == null)
        {
            animator = actor.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(controllerResourcePath);
        animator.applyRootMotion = false;

        if (animator.runtimeAnimatorController == null)
        {
            animator.enabled = false;
            Debug.LogWarning("Animator Controller was not found in Resources: " + controllerResourcePath);
        }

        return animator;
    }

    private SpriteRenderer CreateActorFromPrefabOrFallback(
        string prefabResourcePath,
        string objectName,
        Vector3 position,
        Sprite fallbackSprite,
        int sortingOrder)
    {
        GameObject prefab = Resources.Load<GameObject>(prefabResourcePath);
        if (prefab == null)
        {
            Debug.LogWarning("Character Prefab was not found; using runtime fallback: " + prefabResourcePath);
            return CreateActor(objectName, position, fallbackSprite, sortingOrder);
        }

        GameObject actor = Object.Instantiate(prefab, parent);
        actor.name = objectName;
        actor.transform.position = position;
        actor.transform.localScale = new Vector3(3f, 3f, 1f);

        SpriteRenderer renderer = actor.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = actor.AddComponent<SpriteRenderer>();
        }

        if (renderer.sprite == null)
        {
            renderer.sprite = fallbackSprite;
        }

        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private SpriteRenderer CreateActor(string objectName, Vector3 position, Sprite sprite, int sortingOrder)
    {
        GameObject actor = new GameObject(objectName);
        actor.transform.SetParent(parent, false);
        actor.transform.position = position;
        actor.transform.localScale = new Vector3(3f, 3f, 1f);

        SpriteRenderer renderer = actor.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private static Sprite CreateHeroSprite()
    {
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color outline = new Color(0.05f, 0.06f, 0.08f, 1f);
        Color skin = new Color(0.95f, 0.72f, 0.52f, 1f);
        Color armor = new Color(0.28f, 0.58f, 0.95f, 1f);
        Color boots = new Color(0.16f, 0.12f, 0.1f, 1f);
        Texture2D texture = CreatePixelTexture(16, 16, clear);

        Fill(texture, 6, 11, 4, 3, skin);
        Fill(texture, 5, 10, 6, 1, outline);
        Fill(texture, 5, 6, 6, 5, armor);
        Fill(texture, 3, 7, 2, 4, armor);
        Fill(texture, 11, 7, 2, 4, armor);
        Fill(texture, 5, 2, 2, 4, boots);
        Fill(texture, 9, 2, 2, 4, boots);
        Fill(texture, 12, 8, 3, 1, new Color(0.82f, 0.84f, 0.86f, 1f));
        return ToSprite(texture);
    }

    private static Sprite CreateMonsterSprite()
    {
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color slime = new Color(0.32f, 0.86f, 0.56f, 1f);
        Color dark = new Color(0.05f, 0.16f, 0.09f, 1f);
        Texture2D texture = CreatePixelTexture(16, 16, clear);

        Fill(texture, 3, 3, 10, 6, slime);
        Fill(texture, 4, 9, 8, 3, slime);
        Fill(texture, 6, 7, 1, 1, dark);
        Fill(texture, 10, 7, 1, 1, dark);
        Fill(texture, 7, 4, 3, 1, dark);
        return ToSprite(texture);
    }

    private static Sprite CreateSolidSprite(
        string textureName,
        int width,
        int height,
        Color color,
        float pixelsPerUnit = 16f)
    {
        Texture2D texture = CreatePixelTexture(width, height, color);
        texture.name = textureName;
        return ToSprite(texture, pixelsPerUnit);
    }

    private static Texture2D CreatePixelTexture(int width, int height, Color fill)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, y, fill);
            }
        }

        texture.Apply();
        return texture;
    }

    private static void Fill(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        for (int py = y; py < y + height; py++)
        {
            for (int px = x; px < x + width; px++)
            {
                texture.SetPixel(px, py, color);
            }
        }

        texture.Apply();
    }

    private static Sprite ToSprite(Texture2D texture, float pixelsPerUnit = 16f)
    {
        // FullRect Mesh를 사용하면 Sliced SpriteRenderer에서 Tiling 경고가 발생하지 않습니다.
        return Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0f),
            pixelsPerUnit,
            0,
            SpriteMeshType.FullRect);
    }

    private static float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
