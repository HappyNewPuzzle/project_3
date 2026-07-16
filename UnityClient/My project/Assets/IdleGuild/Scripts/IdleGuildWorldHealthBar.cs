using UnityEngine;

// SpriteRenderer 두 개로 캐릭터 머리 위에 표시되는 간단한 월드 체력 바입니다.
public sealed class IdleGuildWorldHealthBar
{
    private const float BarWidth = 1.8f;
    private const float BarHeight = 0.14f;
    private readonly Transform fill;
    private readonly Vector3 fillCenter;

    public IdleGuildWorldHealthBar(
        Transform actor,
        Sprite pixelSprite,
        string objectName,
        Color fillColor)
    {
        // Prefab을 우선 사용하고, 아직 생성되지 않았다면 기존 코드 생성 방식으로 돌아갑니다.
        GameObject prefab = Resources.Load<GameObject>("Prefabs/UI/WorldHealthBar");
        GameObject root = prefab != null
            ? Object.Instantiate(prefab, actor)
            : CreateFallbackRoot(actor);
        root.name = objectName;
        root.transform.SetParent(actor, false);
        root.transform.localPosition = new Vector3(0f, 0.82f, 0f);

        // 캐릭터가 3배 확대되어 있으므로 자식 HUD는 역배율로 원래 월드 크기를 유지합니다.
        Vector3 actorScale = actor.localScale;
        root.transform.localScale = new Vector3(
            SafeInverse(actorScale.x),
            SafeInverse(actorScale.y),
            1f);

        Transform background = FindOrCreatePart(root.transform, "Background", pixelSprite, 19);
        SpriteRenderer backgroundRenderer = background.GetComponent<SpriteRenderer>();
        backgroundRenderer.sprite = pixelSprite;
        backgroundRenderer.color = new Color(0.04f, 0.05f, 0.06f, 0.95f);
        backgroundRenderer.sortingOrder = 19;
        background.localScale = new Vector3(BarWidth + 0.12f, BarHeight + 0.1f, 1f);

        fill = FindOrCreatePart(root.transform, "Fill", pixelSprite, 20);
        SpriteRenderer fillRenderer = fill.GetComponent<SpriteRenderer>();
        fillRenderer.sprite = pixelSprite;
        fillRenderer.color = fillColor;
        fillRenderer.sortingOrder = 20;
        fillCenter = Vector3.zero;
        SetHealth(1, 1);
    }

    // 현재 HP를 0~1 비율로 바꿔 Fill의 폭과 중심을 함께 갱신합니다.
    public void SetHealth(int currentHealth, int maxHealth)
    {
        float ratio = maxHealth <= 0
            ? 0f
            : Mathf.Clamp01((float)currentHealth / maxHealth);
        float visibleWidth = BarWidth * ratio;

        fill.localScale = new Vector3(visibleWidth, BarHeight, 1f);
        // 중심 Pivot Sprite가 왼쪽 끝을 고정한 것처럼 보이도록 줄어든 폭의 절반만큼 이동합니다.
        fill.localPosition = fillCenter + new Vector3((visibleWidth - BarWidth) * 0.5f, 0f, -0.01f);
    }

    private static GameObject CreateFallbackRoot(Transform actor)
    {
        GameObject root = new GameObject("WorldHealthBar Fallback");
        root.transform.SetParent(actor, false);
        return root;
    }

    private static Transform FindOrCreatePart(
        Transform parent,
        string objectName,
        Sprite pixelSprite,
        int sortingOrder)
    {
        Transform part = parent.Find(objectName);
        if (part == null)
        {
            GameObject partObject = new GameObject(objectName);
            partObject.transform.SetParent(parent, false);
            part = partObject.transform;
        }

        SpriteRenderer renderer = part.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = part.gameObject.AddComponent<SpriteRenderer>();
        }

        renderer.sprite = pixelSprite;
        renderer.sortingOrder = sortingOrder;
        return part;
    }

    private static float SafeInverse(float value)
    {
        return Mathf.Approximately(value, 0f) ? 1f : 1f / value;
    }
}
