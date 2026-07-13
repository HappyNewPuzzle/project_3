using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Sprite Sheet 분할, Animation Clip, Animator Controller 생성을 자동화하는 Editor 전용 도구입니다.
[InitializeOnLoad]
public static class IdleGuildAnimationAssetBuilder
{
    // 원본 PNG와 생성할 Animator 애셋의 프로젝트 상대 경로입니다.
    private const string HeroTexturePath = "Assets/IdleGuild/Resources/Sprites/hero-spritesheet.png";
    private const string SlimeTexturePath = "Assets/IdleGuild/Resources/Sprites/slime-spritesheet.png";
    private const string AnimationRoot = "Assets/IdleGuild/Resources/Animations";
    private const string HeroControllerPath = AnimationRoot + "/Hero/HeroAnimator.controller";
    private const string SlimeControllerPath = AnimationRoot + "/Slime/SlimeAnimator.controller";
    private const string StateParameter = "State";
    private const int Columns = 4;
    private const int Rows = 4;

    // Sprite Sheet 위쪽 행부터 사용하는 상태 이름이며 런타임 enum 순서와 같습니다.
    private static readonly string[] StateNames = { "Idle", "Run", "Attack", "Hit" };
    private static bool isBuilding;

    // 스크립트가 컴파일된 뒤 애셋이 없다면 한 번 자동 생성합니다.
    static IdleGuildAnimationAssetBuilder()
    {
        EditorApplication.delayCall += EnsureAnimationAssets;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    // Unity 상단 메뉴에서 애셋을 수동으로 다시 만들 수 있게 합니다.
    [MenuItem("Idle Guild/Rebuild Character Animation Assets")]
    public static void RebuildAnimationAssets()
    {
        BuildAnimationAssets(true);
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Play 중에는 AssetDatabase를 수정하지 않고 Edit Mode로 돌아왔을 때 생성합니다.
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += EnsureAnimationAssets;
        }
    }

    private static void EnsureAnimationAssets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            return;
        }

        bool heroExists = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(HeroControllerPath) != null;
        bool slimeExists = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SlimeControllerPath) != null;
        if (!heroExists || !slimeExists)
        {
            BuildAnimationAssets(false);
        }
    }

    private static void BuildAnimationAssets(bool forceRebuild)
    {
        if (isBuilding)
        {
            return;
        }

        isBuilding = true;
        try
        {
            EnsureFolder(AnimationRoot);
            BuildCharacter("hero", HeroTexturePath, AnimationRoot + "/Hero", HeroControllerPath, forceRebuild);
            BuildCharacter("slime", SlimeTexturePath, AnimationRoot + "/Slime", SlimeControllerPath, forceRebuild);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Idle Guild character Animation Clips and Animator Controllers are ready.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            isBuilding = false;
        }
    }

    private static void BuildCharacter(
        string characterName,
        string texturePath,
        string outputFolder,
        string controllerPath,
        bool forceRebuild)
    {
        EnsureFolder(outputFolder);
        ConfigureSpriteSheet(characterName, texturePath);

        // 재임포트된 PNG의 16개 Sprite 하위 애셋을 이름으로 찾습니다.
        Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath)
            .OfType<Sprite>()
            .ToDictionary(sprite => sprite.name, sprite => sprite);

        if (sprites.Count != Columns * Rows)
        {
            throw new InvalidOperationException(
                texturePath + " must contain exactly " + (Columns * Rows) + " sprites, but found " + sprites.Count + ".");
        }

        if (forceRebuild)
        {
            AssetDatabase.DeleteAsset(controllerPath);
            foreach (string stateName in StateNames)
            {
                AssetDatabase.DeleteAsset(GetClipPath(outputFolder, characterName, stateName));
            }
        }

        AnimationClip[] clips = new AnimationClip[Rows];
        for (int row = 0; row < Rows; row++)
        {
            string stateName = StateNames[row];
            string clipPath = GetClipPath(outputFolder, characterName, stateName);
            clips[row] = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clips[row] == null)
            {
                clips[row] = CreateAnimationClip(characterName, stateName, row, sprites);
                AssetDatabase.CreateAsset(clips[row], clipPath);
            }
        }

        if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath) == null)
        {
            CreateAnimatorController(controllerPath, clips);
        }
    }

    private static void ConfigureSpriteSheet(string characterName, string texturePath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
        {
            throw new FileNotFoundException("Sprite Sheet importer was not found.", texturePath);
        }

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
        {
            throw new FileNotFoundException("Sprite Sheet texture was not found.", texturePath);
        }

        int frameWidth = texture.width / Columns;
        int frameHeight = texture.height / Rows;
        SpriteMetaData[] metadata = new SpriteMetaData[Columns * Rows];

        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                string stateName = StateNames[row].ToLowerInvariant();
                int index = row * Columns + column;
                metadata[index] = new SpriteMetaData
                {
                    name = characterName + "_" + stateName + "_" + column,
                    rect = new Rect(
                        column * frameWidth,
                        texture.height - (row + 1) * frameHeight,
                        frameWidth,
                        frameHeight),
                    alignment = (int)SpriteAlignment.BottomCenter,
                    pivot = new Vector2(0.5f, 0f),
                    border = Vector4.zero
                };
            }
        }

        // Pixel Art에 맞는 Import 설정과 Multiple Sprite 분할 정보를 저장합니다.
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = frameWidth;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
#pragma warning disable 0618
        importer.spritesheet = metadata;
#pragma warning restore 0618
        importer.SaveAndReimport();
    }

    private static AnimationClip CreateAnimationClip(
        string characterName,
        string stateName,
        int row,
        IReadOnlyDictionary<string, Sprite> sprites)
    {
        float frameRate = stateName == "Idle" ? 6f : 10f;
        AnimationClip clip = new AnimationClip
        {
            name = characterName + stateName,
            frameRate = frameRate
        };

        bool shouldLoop = stateName == "Idle" || stateName == "Run";
        // 마지막 프레임에도 한 프레임 분량의 표시 시간을 주기 위해 종료 키를 하나 더 둡니다.
        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[Columns + 1];
        for (int column = 0; column < Columns; column++)
        {
            string spriteName = characterName + "_" + StateNames[row].ToLowerInvariant() + "_" + column;
            keyframes[column] = new ObjectReferenceKeyframe
            {
                time = column / frameRate,
                value = sprites[spriteName]
            };
        }

        keyframes[Columns] = new ObjectReferenceKeyframe
        {
            time = Columns / frameRate,
            // 반복 Clip은 첫 프레임으로 연결하고, 1회 Clip은 마지막 자세를 유지합니다.
            value = shouldLoop ? keyframes[0].value : keyframes[Columns - 1].value
        };

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        // Idle과 Run은 반복하고 Attack과 Hit는 한 번만 재생하도록 설정합니다.
        SerializedObject serializedClip = new SerializedObject(clip);
        SerializedProperty settings = serializedClip.FindProperty("m_AnimationClipSettings");
        settings.FindPropertyRelative("m_LoopTime").boolValue = shouldLoop;
        serializedClip.ApplyModifiedPropertiesWithoutUndo();
        return clip;
    }

    private static void CreateAnimatorController(string controllerPath, IReadOnlyList<AnimationClip> clips)
    {
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter(StateParameter, AnimatorControllerParameterType.Int);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        for (int index = 0; index < StateNames.Length; index++)
        {
            AnimatorState state = stateMachine.AddState(StateNames[index], new Vector3(280f, 70f + index * 70f, 0f));
            state.motion = clips[index];

            // int State 값이 바뀌면 Any State에서 해당 애니메이션 상태로 즉시 전환합니다.
            AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(state);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.Equals, index, StateParameter);

            if (index == 0)
            {
                stateMachine.defaultState = state;
            }
        }
    }

    private static string GetClipPath(string folder, string characterName, string stateName)
    {
        return folder + "/" + characterName + stateName + ".anim";
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parentFolder = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string folderName = Path.GetFileName(folderPath);
        if (string.IsNullOrEmpty(parentFolder))
        {
            throw new InvalidOperationException("Invalid Unity folder path: " + folderPath);
        }

        EnsureFolder(parentFolder);
        AssetDatabase.CreateFolder(parentFolder, folderName);
    }
}
