using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Scans the Animations folder for .anim clips and provides a GUI button for each.
/// Attach to the same root robot GameObject as StretchKeyboardController.
/// </summary>
public class StretchAnimationPlayer : MonoBehaviour
{
    public string folder = "Animations";

    private AnimationClip[] clips = new AnimationClip[0];
    private string[] clipNames = new string[0];

    private Animation animationComponent;
    private StretchKeyboardController keyboardController;
    private bool playing = false;
    private string currentClipName = "";
    private Vector2 scrollPos;

    void Start()
    {
        keyboardController = GetComponent<StretchKeyboardController>();
        ScanClips();
    }

    void Update()
    {
        if (playing && animationComponent != null && !animationComponent.isPlaying)
        {
            StopPlayback();
        }
    }

    public void ScanClips()
    {
#if UNITY_EDITOR
        string folderPath = $"Assets/{folder}";
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folderPath });

        clips = new AnimationClip[guids.Length];
        clipNames = new string[guids.Length];

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            clips[i] = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            clipNames[i] = clips[i] != null ? clips[i].name : "unknown";
        }

        Debug.Log($"StretchAnimationPlayer: Found {clips.Length} clips in {folderPath}");
#endif
    }

    public void PlayClip(AnimationClip clip)
    {
        if (clip == null) return;

        // Stop current playback
        if (playing) StopPlayback();

        // Disable keyboard control
        if (keyboardController != null)
            keyboardController.enabled = false;

        // Disable recorder if present
        var recorder = GetComponent<StretchAnimationRecorder>();
        if (recorder != null)
            recorder.enabled = false;

        // Set up Animation component
        animationComponent = GetComponent<Animation>();
        if (animationComponent == null)
            animationComponent = gameObject.AddComponent<Animation>();

        clip.legacy = true;
        animationComponent.AddClip(clip, clip.name);
        animationComponent.Play(clip.name);

        playing = true;
        currentClipName = clip.name;
        Debug.Log($"StretchAnimationPlayer: Playing {clip.name} ({clip.length:F1}s)");
    }

    public void StopPlayback()
    {
        if (!playing) return;

        playing = false;

        if (animationComponent != null)
            animationComponent.Stop();

        // Re-enable keyboard control
        if (keyboardController != null)
            keyboardController.enabled = true;

        // Re-enable recorder
        var recorder = GetComponent<StretchAnimationRecorder>();
        if (recorder != null)
            recorder.enabled = true;

        currentClipName = "";
        Debug.Log("StretchAnimationPlayer: Playback stopped");
    }

    void OnGUI()
    {
        float btnWidth = 180f;
        float btnHeight = 32f;
        float padding = 6f;
        float panelX = 10f;
        float panelY = 10f;

        GUI.skin.button.fontSize = 14;
        GUI.skin.label.fontSize = 14;

        // Title
        GUI.Label(new Rect(panelX, panelY, btnWidth, 25), "<b>Animation Player</b>");
        panelY += 30;

        // Refresh button
        if (GUI.Button(new Rect(panelX, panelY, btnWidth, btnHeight), "Refresh"))
            ScanClips();
        panelY += btnHeight + padding;

        // Stop button when playing
        if (playing)
        {
            GUI.color = Color.yellow;
            if (GUI.Button(new Rect(panelX, panelY, btnWidth, btnHeight), $"Stop: {currentClipName}"))
                StopPlayback();
            GUI.color = Color.white;
            panelY += btnHeight + padding;
        }

        // Scrollable clip list
        float listHeight = Mathf.Min(clips.Length * (btnHeight + padding), 400f);
        scrollPos = GUI.BeginScrollView(
            new Rect(panelX, panelY, btnWidth + 20, listHeight),
            scrollPos,
            new Rect(0, 0, btnWidth, clips.Length * (btnHeight + padding))
        );

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] == null) continue;

            float btnY = i * (btnHeight + padding);
            bool isCurrent = playing && clips[i].name == currentClipName;

            if (isCurrent)
                GUI.color = Color.green;

            if (GUI.Button(new Rect(0, btnY, btnWidth, btnHeight), $"Play {clipNames[i]}"))
            {
                PlayClip(clips[i]);
            }

            if (isCurrent)
                GUI.color = Color.white;
        }

        GUI.EndScrollView();
    }
}
