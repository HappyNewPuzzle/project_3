using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class IdleGuildBalanceAssetBuilder
{
    private const string Folder = "Assets/IdleGuild/Resources/Balance";
    private const string AssetPath = Folder + "/IdleGuildBalance.asset";

    static IdleGuildBalanceAssetBuilder()
    {
        EditorApplication.delayCall += EnsureAsset;
    }

    [MenuItem("Idle Guild/Create or Select Balance Config")]
    public static void EnsureAsset()
    {
        if (AssetDatabase.LoadAssetAtPath<IdleGuildBalanceConfig>(AssetPath) == null)
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/IdleGuild/Resources"))
                    AssetDatabase.CreateFolder("Assets/IdleGuild", "Resources");
                AssetDatabase.CreateFolder("Assets/IdleGuild/Resources", "Balance");
            }

            IdleGuildBalanceConfig config = ScriptableObject.CreateInstance<IdleGuildBalanceConfig>();
            AssetDatabase.CreateAsset(config, AssetPath);
            AssetDatabase.SaveAssets();
        }

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<IdleGuildBalanceConfig>(AssetPath);
    }
}
