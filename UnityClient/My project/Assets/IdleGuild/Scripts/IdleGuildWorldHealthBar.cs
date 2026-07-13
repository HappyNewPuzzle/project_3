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
        // 체력 바 Root를 캐릭터 자식으로 만들어 캐릭터 이동을 자동으로 따라가게 합니다.
        GameObject root = new GameObject(objectName);
        root.transform.SetParent(actor, false);
        root.transform.localPosition = new Vector3(0f, 0.82f, 0f);

        // 캐릭터가 3배 확대되어 있으므로 자식 HUD는 역배율로 원래 월드 크기를 유지합니다.
        Vector3 actorScale = actor.localScale;
        root.transform.localScale = new Vector3(
            SafeInverse(actorScale.x),
            SafeInverse(actorScale.y),
            1f);

        // 검은 배경을 Fill보다 조금 크게 만들어 테두리처럼 보이게 합니다.
        GameObject background = CreateBarPart(
            root.transform,
            pixelSprite,
            "Background",
            new Color(0.04f, 0.05f, 0.06f, 0.95f),
            19);
        background.transform.localScale = new Vector3(BarWidth + 0.12f, BarHeight + 0.1f, 1f);

        // 실제 체력 비율을 표시할 Fill Sprite를 배경 앞에 배치합니다.
        GameObject fillObject = CreateBarPart(root.transform, pixelSprite, "Fill", fillColor, 20);
        fill = fillObject.transform;
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

    private static GameObject CreateBarPart(
        Transform parent,
        Sprite pixelSprite,
        string objectName,
        Color color,
        int sortingOrder)
    {
        GameObject part = new GameObject(objectName);
        part.transform.SetParent(parent, false);
        SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = pixelSprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return part;
    }

    private static float SafeInverse(float value)
    {
        return Mathf.Approximately(value, 0f) ? 1f : 1f / value;
    }
}
