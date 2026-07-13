using System.Collections;
using UnityEngine;

public sealed class IdleGuildGameWorld
{
    private readonly Transform parent;
    private readonly MonoBehaviour coroutineHost;

    private Transform hero;
    private Transform monster;
    private SpriteRenderer heroRenderer;
    private SpriteRenderer monsterRenderer;
    private Vector3 heroHome;
    private Vector3 monsterHome;
    private Coroutine combatRoutine;

    public IdleGuildGameWorld(Transform parent, MonoBehaviour coroutineHost)
    {
        this.parent = parent;
        this.coroutineHost = coroutineHost;
    }

    public void Build()
    {
        CreateCameraBackdrop();
        CreateGround();
        CreateActors();
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
        hero.position = heroHome;
        monster.position = monsterHome;
        heroRenderer.flipX = false;
        monster.gameObject.SetActive(true);
        monsterRenderer.color = Color.white;

        Vector3 attackPoint = monsterHome + new Vector3(-1.1f, 0f, 0f);
        yield return MoveTo(hero, attackPoint, 0.55f);

        for (int i = 0; i < 3; i++)
        {
            hero.position = attackPoint + new Vector3(0.18f, 0f, 0f);
            monsterRenderer.color = new Color(1f, 0.55f, 0.55f, 1f);
            yield return new WaitForSeconds(0.08f);
            hero.position = attackPoint;
            monsterRenderer.color = Color.white;
            yield return new WaitForSeconds(0.08f);
        }

        if (string.Equals(outcome, "succeeded", System.StringComparison.OrdinalIgnoreCase))
        {
            yield return KnockOutMonster();
        }
        else
        {
            yield return BumpBack(hero);
        }

        yield return MoveTo(hero, heroHome, 0.45f);
        combatRoutine = null;
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

        heroRenderer = CreateActor("Pixel Hero", heroHome, CreateHeroSprite(), 5);
        monsterRenderer = CreateActor("Training Slime", monsterHome, CreateMonsterSprite(), 5);

        hero = heroRenderer.transform;
        monster = monsterRenderer.transform;
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
