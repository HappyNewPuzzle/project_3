using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class IdleGuildAndroidBuild
{
    [MenuItem("Idle Guild/Build Android/Development APK")]
    public static void BuildTestApk()
    {
        BuildAndroid("IdleGuild-development.apk", true);
    }

    [MenuItem("Idle Guild/Build Android/Optimized Test APK")]
    public static void BuildOptimizedTestApk()
    {
        BuildAndroid("IdleGuild-optimized-test.apk", false);
    }

    private static void BuildAndroid(string fileName, bool development)
    {
        string output = Path.GetFullPath(Path.Combine("Builds/Android", fileName));
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        PlayerSettings.companyName = "IdleGuild";
        PlayerSettings.productName = "Idle Guild";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.idleguild.game");
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.Android.forceInternetPermission = true;
        PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Medium);
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        EditorUserBuildSettings.buildAppBundle = false;
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/IdleGuild/Scenes/MainScene.unity" },
            locationPathName = output,
            target = BuildTarget.Android,
            options = development
                ? BuildOptions.Development | BuildOptions.AllowDebugging
                : BuildOptions.CompressWithLz4HC,
            extraScriptingDefines = new[] { "IDLE_GUILD_SERVER_BUILD" }
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new System.InvalidOperationException("Android test build failed: " + report.summary.result);
        Debug.Log("Android " + (development ? "development" : "optimized test") + " APK: " + output);
    }
}
