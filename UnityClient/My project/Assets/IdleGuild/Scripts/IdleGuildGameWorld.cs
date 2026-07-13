using System.Collections;
using UnityEngine;

public sealed class IdleGuildGameWorld
{
    // Sprite Sheet는 가로 4프레임, 세로 4개 애니메이션 행으로 구성됩니다.
    private const int SheetColumns = 4;
    private const int SheetRows = 4;
    // 각 행의 의미를 숫자 대신 이름으로 사용하기 위한 상수입니다.
    private const int IdleRow = 0;
    private const int RunRow = 1;
    private const int AttackRow = 2;
    private const int HitRow = 3;

    private readonly Transform parent;
    private readonly MonoBehaviour coroutineHost;

    private Transform hero;
    private Transform monster;
    private SpriteRenderer heroRenderer;
    private SpriteRenderer monsterRenderer;
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
        // 이전 Idle 애니메이션을 멈추고 전투 시작 상태로 되돌립니다.
        StopActorAnimations();
        hero.position = heroHome;
        monster.position = monsterHome;
        heroRenderer.flipX = false;
        monster.gameObject.SetActive(true);
        monsterRenderer.color = Color.white;
        monsterAnimationRoutine = coroutineHost.StartCoroutine(
            PlayAnimation(monsterRenderer, monsterFrames, IdleRow, 0.16f, true));

        // Run 행을 반복 재생하면서 영웅의 Transform을 몬스터 앞으로 이동시킵니다.
        Vector3 attackPoint = monsterHome + new Vector3(-1.1f, 0f, 0f);
        heroAnimationRoutine = coroutineHost.StartCoroutine(
            PlayAnimation(heroRenderer, heroFrames, RunRow, 0.09f, true));
        yield return MoveTo(hero, attackPoint, 0.55f);
        StopHeroAnimation();

        // Attack 행 네 프레임을 한 번 재생해 실제 이미지 기반 공격 동작을 보여줍니다.
        yield return PlayAnimation(heroRenderer, heroFrames, AttackRow, 0.10f, false);

        if (string.Equals(outcome, "succeeded", System.StringComparison.OrdinalIgnoreCase))
        {
            // 승리하면 슬라임의 Hit 행을 재생한 뒤 밀려나며 사라지게 합니다.
            StopMonsterAnimation();
            yield return PlayAnimation(monsterRenderer, monsterFrames, HitRow, 0.10f, false);
            yield return KnockOutMonster();
        }
        else
        {
            // 실패하면 영웅의 Hit 행을 재생하고 원래 공격 위치로 튕겨 돌아옵니다.
            yield return PlayAnimation(heroRenderer, heroFrames, HitRow, 0.10f, false);
            yield return BumpBack(hero);
        }

        // 복귀할 때는 왼쪽을 보도록 flipX를 켜고 Run 행을 다시 반복합니다.
        heroRenderer.flipX = true;
        heroAnimationRoutine = coroutineHost.StartCoroutine(
            PlayAnimation(heroRenderer, heroFrames, RunRow, 0.09f, true));
        yield return MoveTo(hero, heroHome, 0.45f);
        StopHeroAnimation();
        heroRenderer.flipX = false;

        // 전투가 끝나면 살아 있는 캐릭터를 다시 Idle 애니메이션으로 전환합니다.
        StartIdleAnimations();
        combatRoutine = null;
    }

    // 지정한 Sprite Sheet 행의 네 프레임을 순서대로 재생합니다.
    private static IEnumerator PlayAnimation(
        SpriteRenderer renderer,
        Sprite[] frames,
        int row,
        float frameDuration,
        bool loop)
    {
        // 이미지 로드 실패 시 fallback Sprite를 유지하고 한 프레임만 기다립니다.
        if (frames == null || frames.Length != SheetColumns * SheetRows)
        {
            yield return null;
            yield break;
        }

        int firstFrame = row * SheetColumns;
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

    // 영웅과 활성 상태인 몬스터의 대기 동작을 시작합니다.
    private void StartIdleAnimations()
    {
        StopActorAnimations();
        heroAnimationRoutine = coroutineHost.StartCoroutine(
            PlayAnimation(heroRenderer, heroFrames, IdleRow, 0.18f, true));

        if (monster.gameObject.activeSelf)
        {
            monsterAnimationRoutine = coroutineHost.StartCoroutine(
                PlayAnimation(monsterRenderer, monsterFrames, IdleRow, 0.18f, true));
        }
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

        hero = heroRenderer.transform;
        monster = monsterRenderer.transform;
    }

    // Resources의 4x4 PNG를 위에서 아래, 왼쪽에서 오른쪽 순서의 Sprite 배열로 나눕니다.
    private static Sprite[] LoadSpriteSheet(string resourcePath)
    {
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

    private static Sprite CreateSolidSprite(string textureName, int width, int height, Color color)
    {
        Texture2D texture = CreatePixelTexture(width, height, color);
        texture.name = textureName;
        return ToSprite(texture);
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

    private static Sprite ToSprite(Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0f), 16f);
    }

    private static float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
