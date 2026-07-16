using System;
using UnityEditor;
using UnityEngine;

// 캐릭터의 표시 컴포넌트를 재사용 가능한 Resources Prefab으로 생성합니다.
public static class IdleGuildCharacterPrefabBuilder
{
    private const string PrefabRoot = "Assets/IdleGuild/Resources/Prefabs/Characters";

    [MenuItem("Idle Guild/Rebuild Character Prefabs")]
    public static void RebuildCharacterPrefabs()
    {
        EnsureFolder("Assets/IdleGuild/Resources/Prefabs");
        EnsureFolder(PrefabRoot);

        BuildPrefab(
            "ClassicHero",
            "Assets/IdleGuild/Resources/Sprites/hero-spritesheet.png",
            "Assets/IdleGuild/Resources/Animations/Hero/HeroAnimator.controller");
        BuildPrefab(
            "Slime",
            "Assets/IdleGuild/Resources/Sprites/slime-spritesheet.png",
            "Assets/IdleGuild/Resources/Animations/Slime/SlimeAnimator.controller");
        BuildPrefab(
            "GirlHero",
            "Assets/IdleGuild/Resources/Sprites/girl-hero-spritesheet-v5.png",
            "Assets/IdleGuild/Resources/Animations/GirlHeroV5/GirlHeroAnimator.controller");
        BuildPrefab(
            "MaskedThief",
            "Assets/IdleGuild/Resources/Sprites/masked-thief-spritesheet.png",
            "Assets/IdleGuild/Resources/Animations/MaskedThief/MaskedThiefAnimator.controller");
        BuildPrefab(
            "BlackCat",
            "Assets/IdleGuild/Resources/Sprites/black-cat-red-ribbon-spritesheet.png",
            "Assets/IdleGuild/Resources/Animations/BlackCat/BlackCatAnimator.controller");

        AssetDatabase.SaveAssets();
        Debug.Log("Idle Guild character Prefabs are ready.");
    }

    private static void BuildPrefab(string prefabName, string spriteSheetPath, string controllerPath)
    {
        Sprite firstFrame = FindFirstFrame(spriteSheetPath);
        RuntimeAnimatorController controller =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        if (firstFrame == null || controller == null)
        {
            throw new InvalidOperationException("Character assets are incomplete for Prefab: " + prefabName);
        }

        GameObject root = new GameObject(prefabName);
        try
        {
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = firstFrame;
            renderer.sortingOrder = 5;

            Animator animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabRoot + "/" + prefabName + ".prefab");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static Sprite FindFirstFrame(string spriteSheetPath)
    {
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath))
        {
            Sprite sprite = asset as Sprite;
            if (sprite != null && sprite.name.EndsWith("_idle_0", StringComparison.Ordinal))
            {
                return sprite;
            }
        }

        return null;
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
