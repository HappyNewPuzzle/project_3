using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 월드 공간 UI의 공통 계층 구조를 Resources Prefab으로 생성합니다.
public static class IdleGuildUiPrefabBuilder
{
    private const string PrefabFolder = "Assets/IdleGuild/Resources/Prefabs/UI";
    private const string HealthBarPath = PrefabFolder + "/WorldHealthBar.prefab";
    private const string RuntimeCanvasPath = PrefabFolder + "/RuntimeCanvas.prefab";

    [MenuItem("Idle Guild/Rebuild UI Prefabs")]
    public static void RebuildUiPrefabs()
    {
        EnsureFolder("Assets/IdleGuild/Resources/Prefabs");
        EnsureFolder(PrefabFolder);

        BuildWorldHealthBar();
        BuildRuntimeCanvas();
        AssetDatabase.SaveAssets();
        Debug.Log("Idle Guild UI Prefabs are ready.");
    }

    private static void BuildWorldHealthBar()
    {
        GameObject root = new GameObject("WorldHealthBar");
        try
        {
            CreatePart(root.transform, "Background", 19);
            CreatePart(root.transform, "Fill", 20);
            PrefabUtility.SaveAsPrefabAsset(root, HealthBarPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void BuildRuntimeCanvas()
    {
        GameObject root = new GameObject(
            "RuntimeCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        try
        {
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            PrefabUtility.SaveAsPrefabAsset(root, RuntimeCanvasPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void CreatePart(Transform parent, string name, int sortingOrder)
    {
        GameObject part = new GameObject(name);
        part.transform.SetParent(parent, false);
        SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = sortingOrder;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        int separator = folderPath.LastIndexOf('/');
        string parent = folderPath.Substring(0, separator);
        string name = folderPath.Substring(separator + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
