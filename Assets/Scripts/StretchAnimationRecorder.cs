using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;

[CustomEditor(typeof(StretchAnimationRecorder))]
class StretchAnimationRecorderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        StretchAnimationRecorder recorder = (StretchAnimationRecorder)target;

        GUILayout.BeginHorizontal();
        GUILayout.Label("Clip Name");
        recorder.clipName = GUILayout.TextField(recorder.clipName);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Start Recording"))
        {
            recorder.StartRecording();
        }
        if (GUILayout.Button("Stop Recording"))
        {
            recorder.StopRecording();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Replay"))
        {
            recorder.StartReplay();
        }
        if (GUILayout.Button("Stop Replay"))
        {
            recorder.StopReplay();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        DrawDefaultInspector();
    }
}
#endif

/// <summary>
/// Records and replays the Stretch SE3 robot animation using GameObjectRecorder.
/// Saves recordings as .anim AnimationClip assets under Assets/Animations/.
/// Replays using Unity's Animation component.
/// Attach to the root robot GameObject.
/// </summary>
public class StretchAnimationRecorder : MonoBehaviour
{
#if UNITY_EDITOR
    private GameObjectRecorder m_Recorder;
#endif

    public string clipName = "stretch_record";
    public string folder = "Animations";

    [SerializeField, HideInInspector]
    private bool recording = false;
    [SerializeField, HideInInspector]
    private bool replaying = false;
    [SerializeField, HideInInspector]
    private string lastSaved = "";

    private Animation animationComponent;
    private StretchKeyboardController keyboardController;
    private string statusMessage = "";

    void Start()
    {
        keyboardController = GetComponent<StretchKeyboardController>();
        InitRecorder();
    }

    private void InitRecorder()
    {
#if UNITY_EDITOR
        if (m_Recorder != null)
            DestroyImmediate(m_Recorder);

        m_Recorder = new GameObjectRecorder(gameObject);
        m_Recorder.BindComponentsOfType<Transform>(gameObject, true);
#endif
    }

    void LateUpdate()
    {
#if UNITY_EDITOR
        if (!recording || m_Recorder == null)
            return;

        m_Recorder.TakeSnapshot(Time.deltaTime);
#endif
    }

    void Update()
    {
        // Check if replay animation finished
        if (replaying && animationComponent != null && !animationComponent.isPlaying)
        {
            StopReplay();
        }
    }

#if UNITY_EDITOR
    private AnimationClip CreateAndSaveClip()
    {
        // Ensure folder exists
        string folderPath = $"Assets/{folder}";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", folder);
        }

        AnimationClip clip = new AnimationClip();
        string fullPath = $"{folderPath}/{clipName}.anim";

        // Handle duplicate names
        fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);

        AssetDatabase.CreateAsset(clip, fullPath);
        AssetDatabase.SaveAssets();

        return clip;
    }
#endif

    public void StartRecording()
    {
#if UNITY_EDITOR
        if (recording) return;

        // Stop any ongoing replay
        if (replaying) StopReplay();

        InitRecorder();
        recording = true;
        statusMessage = "Recording...";
        Debug.Log("StretchAnimationRecorder: Recording started");
#else
        Debug.LogWarning("StretchAnimationRecorder: Recording is only available in the Unity Editor.");
#endif
    }

    public void StopRecording()
    {
#if UNITY_EDITOR
        if (!recording) return;

        recording = false;

        if (m_Recorder != null && m_Recorder.isRecording)
        {
            var clip = CreateAndSaveClip();
            m_Recorder.SaveToClip(clip);

            // Re-save after SaveToClip populates the curves
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();

            lastSaved = clip.name;
            statusMessage = $"Saved: {lastSaved}.anim";
            Debug.Log($"StretchAnimationRecorder: Saved recording to {AssetDatabase.GetAssetPath(clip)}");
        }
        else
        {
            statusMessage = "Nothing recorded.";
        }

        InitRecorder();
#endif
    }

    public void StartReplay()
    {
#if UNITY_EDITOR
        if (replaying) return;
        if (recording) StopRecording();

        // Find the clip to replay
        string clipPath = $"Assets/{folder}/{clipName}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

        if (clip == null)
        {
            // Try the last saved clip
            if (!string.IsNullOrEmpty(lastSaved))
            {
                clipPath = $"Assets/{folder}/{lastSaved}.anim";
                clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            }

            if (clip == null)
            {
                statusMessage = "No animation clip found!";
                Debug.LogWarning($"StretchAnimationRecorder: Clip not found at {clipPath}");
                return;
            }
        }

        // Disable keyboard control during replay
        if (keyboardController != null)
            keyboardController.enabled = false;

        // Set up Animation component for playback
        animationComponent = GetComponent<Animation>();
        if (animationComponent == null)
            animationComponent = gameObject.AddComponent<Animation>();

        clip.legacy = true;
        animationComponent.AddClip(clip, clip.name);
        animationComponent.Play(clip.name);

        replaying = true;
        statusMessage = $"Replaying: {clip.name}";
        Debug.Log($"StretchAnimationRecorder: Replaying {clip.name} ({clip.length:F1}s)");
#else
        Debug.LogWarning("StretchAnimationRecorder: Replay is only available in the Unity Editor.");
#endif
    }

    public void StopReplay()
    {
        if (!replaying) return;

        replaying = false;

        if (animationComponent != null)
        {
            animationComponent.Stop();
        }

        // Re-enable keyboard control
        if (keyboardController != null)
            keyboardController.enabled = true;

        statusMessage = "Replay finished.";
        Debug.Log("StretchAnimationRecorder: Replay finished");
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        // Auto-save if recording when disabled
        if (recording && m_Recorder != null && m_Recorder.isRecording)
        {
            StopRecording();
        }
#endif
    }

    void OnGUI()
    {
        float btnWidth = 120f;
        float btnHeight = 40f;
        float padding = 10f;
        float x = Screen.width - btnWidth - padding;
        float y = padding;

        GUI.skin.button.fontSize = 16;
        GUI.skin.label.fontSize = 14;

        if (!recording && !replaying)
        {
            if (GUI.Button(new Rect(x, y, btnWidth, btnHeight), "Record"))
                StartRecording();
            y += btnHeight + padding;
            if (GUI.Button(new Rect(x, y, btnWidth, btnHeight), "Replay"))
                StartReplay();
        }
        else if (recording)
        {
            GUI.color = Color.red;
            if (GUI.Button(new Rect(x, y, btnWidth, btnHeight), "Stop Rec"))
                StopRecording();
            GUI.color = Color.white;
        }
        else if (replaying)
        {
            GUI.color = Color.yellow;
            if (GUI.Button(new Rect(x, y, btnWidth, btnHeight), "Stop"))
                StopReplay();
            GUI.color = Color.white;
        }

        // Status label
        y += btnHeight + padding + 10;
        if (!string.IsNullOrEmpty(statusMessage))
        {
            GUI.Label(new Rect(x - 150, y, btnWidth + 150, 30), statusMessage);
        }
    }
}
