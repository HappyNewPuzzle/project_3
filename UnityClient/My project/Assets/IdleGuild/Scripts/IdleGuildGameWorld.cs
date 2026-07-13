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
    private Vector3 heroHome;
    private Vector3 monsterHome;
    private Coroutine combatRoutine;
    private Coroutine heroAnimationRoutine;
    private Coroutine monsterAnimationRoutine;

    public IdleGuildGameWorld(Transform parent, MonoBehaviour coroutineHost)
    {
        this.parent = parent;
        this.coroutineHost = coroutineHost;
    }

    public void Build()
    {
        // 실제 PNG Sprite Sheet를 먼저 불러옵니다. 실패하면 CreateActors에서 임시 Sprite를 사용합니다.
        heroFrames = LoadSpriteSheet("Sprites/hero-spritesheet");
        monsterFrames = LoadSpriteSheet("Sprites/slime-spritesheet");
        CreateCameraBackdrop();
        CreateGround();
        CreateActors();
        CreateBattleHud();
        StartIdleAnimations();
    }

    public void PlayStageChallenge(int stage, string outcome)
    {
        if (combatRoutine != null)
        {
            coroutineHost.StopCoroutine(combatRoutine);
        }

        combatRoutine = coroutineHost.StartCoroutine(PlayCombat(stage, outcome));
    }

    private IEnumerator PlayCombat(int stage, string outcome)
    {
        // 서버 결과를 HUD와 전투 연출에서 사용할 클라이언트 전용 표시 데이터로 변환합니다.
        IdleGuildBattlePresentation presentation = IdleGuildBattlePresentation.Create(stage, outcome);
        // 이전 Idle 애니메이션을 멈추고 전투 시작 상태로 되돌립니다.
        StopActorAnimations();
        hero.position = heroHome;
        monster.position = monsterHome;
        heroRenderer.flipX = false;
        monster.gameObject.SetActive(true);
        monsterRenderer.color = Color.white;
        PrepareBattleHud(presentation);
        StartMonsterLoop(ActorAnimationState.Idle, 0.16f);

        // Run 행을 반복 재생하면서 영웅의 Transform을 몬스터 앞으로 이동시킵니다.
        Vector3 attackPoint = monsterHome + new Vector3(-1.1f, 0f, 0f);
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
        const int particleCount = 8;
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
            particle.transform.localScale = new Vector3(0.16f, 0.16f, 1f);
            particles[index] = particle.transform;

            SpriteRenderer renderer = particle.AddComponent<SpriteRenderer>();
            renderer.sprite = hudPixelSprite;
            renderer.color = index % 2 == 0
                ? new Color(1f, 0.9f, 0.25f, 1f)
                : new Color(1f, 0.45f, 0.18f, 1f);
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

    private IEnumerator KnockOutMonster()
    {
        Vector3 origin = monster.position;
        float elapsed = 0f;
        while (elapsed < 0.35f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / 0.35f);
            monster.position = origin + new Vector3(0.45f * t, 0.35f * t, 0f);
            monsterRenderer.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }

        monster.gameObject.SetActive(false);
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
        backdrop.transform.SetParent(parent, false);
        backdrop.transform.position = new Vector3(0f, 0f, 8f);

        SpriteRenderer renderer = backdrop.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSolidSprite("Backdrop Sprite", 1, 1, new Color(0.12f, 0.17f, 0.22f, 1f));
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = new Vector2(14f, 8f);
        renderer.sortingOrder = -20;
    }

    private void CreateGround()
    {
        GameObject ground = new GameObject("Stone Ground");
        ground.transform.SetParent(parent, false);
        ground.transform.position = new Vector3(0f, -2.1f, 0f);

        SpriteRenderer renderer = ground.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSolidSprite("Ground Sprite", 1, 1, new Color(0.25f, 0.28f, 0.31f, 1f));
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = new Vector2(14f, 0.7f);
        renderer.sortingOrder = -10;
    }

    private void CreateActors()
    {
        heroHome = new Vector3(-3.2f, -1.35f, 0f);
        monsterHome = new Vector3(2.6f, -1.35f, 0f);

        // PNG를 정상 로드했으면 첫 Idle 프레임을, 실패했으면 기존 코드 생성 Sprite를 사용합니다.
        Sprite heroSprite = heroFrames != null ? heroFrames[0] : CreateHeroSprite();
        Sprite monsterSprite = monsterFrames != null ? monsterFrames[0] : CreateMonsterSprite();
        heroRenderer = CreateActor("Pixel Hero", heroHome, heroSprite, 5);
        monsterRenderer = CreateActor("Training Slime", monsterHome, monsterSprite, 5);

        // Resources에 생성된 Controller를 각 캐릭터의 Animator 컴포넌트에 연결합니다.
        heroAnimator = CreateAnimator(heroRenderer.gameObject, "Animations/Hero/HeroAnimator");
        monsterAnimator = CreateAnimator(monsterRenderer.gameObject, "Animations/Slime/SlimeAnimator");

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
    private static Sprite[] LoadSpriteSheet(string resourcePath)
    {
        // Editor 빌더가 분할한 Sprite 하위 애셋이 있으면 이름 규칙대로 먼저 불러옵니다.
        Sprite[] importedFrames = LoadImportedSpriteFrames(resourcePath);
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

    // Multiple Sprite Import로 생성된 하위 Sprite를 상태 행/열 순서의 배열로 정렬합니다.
    private static Sprite[] LoadImportedSpriteFrames(string resourcePath)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
        if (sprites == null || sprites.Length != SheetColumns * SheetRows)
        {
            return null;
        }

        string characterName = resourcePath.EndsWith("hero-spritesheet") ? "hero" : "slime";
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
    private static Animator CreateAnimator(GameObject actor, string controllerResourcePath)
    {
        Animator animator = actor.AddComponent<Animator>();
        animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(controllerResourcePath);
        animator.applyRootMotion = false;

        if (animator.runtimeAnimatorController == null)
        {
            animator.enabled = false;
            Debug.LogWarning("Animator Controller was not found in Resources: " + controllerResourcePath);
        }

        return animator;
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
