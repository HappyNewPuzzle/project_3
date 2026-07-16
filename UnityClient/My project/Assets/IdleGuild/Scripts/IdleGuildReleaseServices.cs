using UnityEngine;

public sealed class IdleGuildReleaseServices : MonoBehaviour
{
    private const string SoundKey = "IdleGuild.Settings.Sound";
    private static AudioSource music;
    private static AudioSource effects;
    public static bool SoundEnabled { get; private set; }

    public static void Install(GameObject host)
    {
        if (host.GetComponent<IdleGuildReleaseServices>() == null) host.AddComponent<IdleGuildReleaseServices>();
    }

    private void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        SoundEnabled = PlayerPrefs.GetInt(SoundKey, 1) == 1;
        music = gameObject.AddComponent<AudioSource>();
        effects = gameObject.AddComponent<AudioSource>();
        music.loop = true;
        music.volume = SoundEnabled ? 0.12f : 0f;
        effects.volume = SoundEnabled ? 0.35f : 0f;
        music.clip = CreateTone("Mountain BGM", 110f, 4f, 0.06f);
        music.Play();
    }

    public static void ToggleSound()
    {
        SoundEnabled = !SoundEnabled;
        PlayerPrefs.SetInt(SoundKey, SoundEnabled ? 1 : 0);
        PlayerPrefs.Save();
        if (music != null) music.volume = SoundEnabled ? 0.12f : 0f;
        if (effects != null) effects.volume = SoundEnabled ? 0.35f : 0f;
    }

    public static void PlayEffect(float frequency = 440f)
    {
        if (!SoundEnabled || effects == null) return;
        effects.PlayOneShot(CreateTone("Effect", frequency, 0.09f, 0.18f));
    }

    private static AudioClip CreateTone(string name, float frequency, float seconds, float gain)
    {
        const int sampleRate = 22050;
        int samples = Mathf.CeilToInt(sampleRate * seconds);
        float[] data = new float[samples];
        for (int index = 0; index < samples; index++)
        {
            float time = (float)index / sampleRate;
            float envelope = Mathf.Min(1f, index / 300f) * Mathf.Min(1f, (samples - index) / 500f);
            data[index] = Mathf.Sin(2f * Mathf.PI * frequency * time) * gain * envelope;
        }
        AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}

public sealed class IdleGuildSafeArea : MonoBehaviour
{
    private Rect lastSafeArea;
    private RectTransform rect;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        Apply();
    }

    private void Update()
    {
        if (lastSafeArea != Screen.safeArea) Apply();
    }

    private void Apply()
    {
        if (rect == null || Screen.width <= 0 || Screen.height <= 0) return;
        lastSafeArea = Screen.safeArea;
        rect.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
        rect.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
