using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// MainScene에 사람이 직접 움직일 수 있는 전투 배치 Anchor를 생성합니다.
[InitializeOnLoad]
public static class IdleGuildSceneLayoutBuilder
{
    private const string MainScenePath = "Assets/IdleGuild/Scenes/MainScene.unity";
    private const string LayoutName = "Battle Scene Layout";

    static IdleGuildSceneLayoutBuilder()
    {
        EditorApplication.delayCall += EnsureLayoutInOpenMainScene;
    }

    [MenuItem("Idle Guild/Create or Repair Battle Scene Layout")]
    public static void CreateOrRepairLayout()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != MainScenePath)
        {
            scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        }

        IdleGuildBattleSceneLayout layout = Object.FindFirstObjectByType<IdleGuildBattleSceneLayout>();
        GameObject root = layout != null ? layout.gameObject : new GameObject(LayoutName);
        if (layout == null)
        {
            layout = root.AddComponent<IdleGuildBattleSceneLayout>();
        }

        Transform hero = FindOrCreate(root.transform, "Hero Spawn", new Vector3(-2.8f, -2.5f, 0f));
        Transform monster = FindOrCreate(root.transform, "Monster Spawn", new Vector3(1.8f, -2.75f, 0f));
        UpgradePreviousDefaultPosition(hero, new Vector3(-3.2f, -1.35f, 0f), new Vector3(-2.8f, -2.5f, 0f));
        UpgradePreviousDefaultPosition(hero, new Vector3(-2.8f, -1.35f, 0f), new Vector3(-2.8f, -2.5f, 0f));
        UpgradePreviousDefaultPosition(monster, new Vector3(2.6f, -1.35f, 0f), new Vector3(1.8f, -2.1f, 0f));
        UpgradePreviousDefaultPosition(monster, new Vector3(1.8f, -1.35f, 0f), new Vector3(1.8f, -2.1f, 0f));
        UpgradePreviousDefaultPosition(monster, new Vector3(1.8f, -2.1f, 0f), new Vector3(1.8f, -2.75f, 0f));
        Transform backdrop = FindOrCreate(root.transform, "Backdrop Anchor", new Vector3(0f, 0f, 8f));
        Transform ground = FindOrCreate(root.transform, "Ground Anchor", new Vector3(0f, -2.1f, 0f));
        layout.Configure(hero, monster, backdrop, ground);

        EditorUtility.SetDirty(layout);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Idle Guild Battle Scene Layout is ready. Move its child Anchors to edit the battle composition.");
    }

    private static void EnsureLayoutInOpenMainScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != MainScenePath)
        {
            return;
        }

        IdleGuildBattleSceneLayout layout = Object.FindFirstObjectByType<IdleGuildBattleSceneLayout>();
        if (layout == null || !layout.IsConfigured)
        {
            CreateOrRepairLayout();
        }
    }

    private static Transform FindOrCreate(Transform parent, string name, Vector3 worldPosition)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            GameObject childObject = new GameObject(name);
            child = childObject.transform;
            child.SetParent(parent, false);
            child.position = worldPosition;
        }

        return child;
    }

    private static void UpgradePreviousDefaultPosition(Transform anchor, Vector3 previousDefault, Vector3 newDefault)
    {
        if ((anchor.position - previousDefault).sqrMagnitude < 0.0001f)
        {
            anchor.position = newDefault;
        }
    }
}
