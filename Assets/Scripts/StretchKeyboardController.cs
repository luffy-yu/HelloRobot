using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Keyboard controller for the Hello Robot Stretch SE3 with SG3 gripper.
/// Attach this script to the root robot GameObject imported from URDF.
///
/// Controls:
///   W/S        - Drive forward / backward
///   A/D        - Turn left / right
///   R/F        - Lift up / down
///   T/G        - Extend / retract arm
///   Z/X        - Wrist yaw left / right
///   U/J        - Wrist pitch up / down
///   I/K        - Wrist roll CW / CCW
///   Left/Right - Head pan left / right
///   Up/Down    - Head tilt up / down
///   O/L        - Open / close gripper
///   Space      - Stop all movement
/// </summary>
public class StretchKeyboardController : MonoBehaviour
{
    [Header("Drive")]
    public float driveSpeed = 5f;
    public float turnSpeed = 2f;

    [Header("Lift")]
    public float liftSpeed = 0.3f;

    [Header("Arm Extension")]
    public float armSpeed = 0.2f;

    [Header("Wrist")]
    public float wristYawSpeed = 1.5f;
    public float wristPitchSpeed = 1.5f;
    public float wristRollSpeed = 1.5f;

    [Header("Head")]
    public float headPanSpeed = 10f;
    public float headTiltSpeed = 10f;

    [Header("Gripper")]
    public float gripperSpeed = 10f;

    // Root articulation body (base_link)
    private ArticulationBody rootBody;

    // Head rotation tracking (direct transform control)
    private float headPanAngle = 0f;
    private float headTiltAngle = 0f;

    // Articulation bodies for each controllable joint
    private ArticulationBody leftWheel;
    private ArticulationBody rightWheel;
    private ArticulationBody lift;
    private ArticulationBody[] armSegments; // l0-l3
    private ArticulationBody wristYaw;
    private ArticulationBody wristPitch;
    private ArticulationBody wristRoll;
    private ArticulationBody headPan;
    private ArticulationBody headTilt;
    private ArticulationBody gripperFingerLeft;
    private ArticulationBody gripperFingerRight;

    void Start()
    {
        // Make the root base_link immovable so it doesn't fly away from reaction forces
        rootBody = GetComponent<ArticulationBody>();
        if (rootBody != null)
            rootBody.immovable = true;

        // Find all articulation bodies by joint name
        ArticulationBody[] allBodies = GetComponentsInChildren<ArticulationBody>();

        foreach (var body in allBodies)
        {
            switch (body.gameObject.name)
            {
                case "link_left_wheel":
                    leftWheel = body;
                    break;
                case "link_right_wheel":
                    rightWheel = body;
                    break;
                case "link_lift":
                    lift = body;
                    break;
                case "link_wrist_yaw":
                    wristYaw = body;
                    break;
                case "link_wrist_pitch":
                    wristPitch = body;
                    break;
                case "link_wrist_roll":
                    wristRoll = body;
                    break;
                case "link_head_pan":
                    headPan = body;
                    break;
                case "link_head_tilt":
                    headTilt = body;
                    break;
                case "link_gripper_finger_left":
                    gripperFingerLeft = body;
                    break;
                case "link_gripper_finger_right":
                    gripperFingerRight = body;
                    break;
            }
        }

        // Arm segments: l3, l2, l1, l0 (all prismatic, extending the arm)
        armSegments = new ArticulationBody[4];
        string[] armNames = { "link_arm_l3", "link_arm_l2", "link_arm_l1", "link_arm_l0" };
        foreach (var body in allBodies)
        {
            for (int i = 0; i < armNames.Length; i++)
            {
                if (body.gameObject.name == armNames[i])
                    armSegments[i] = body;
            }
        }

        // Log all ArticulationBody names for debugging
        foreach (var body in allBodies)
            Debug.Log($"StretchController: Found ArticulationBody on '{body.gameObject.name}'");

        LogMissingJoints();
        LogJointLimits();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        HandleDrive(kb);
        HandleLift(kb);
        HandleArm(kb);
        HandleWrist(kb);
        HandleHead(kb);
        HandleGripper(kb);
    }

    private void HandleDrive(Keyboard kb)
    {
        float forward = 0f;
        float turn = 0f;

        if (kb.wKey.isPressed) forward += driveSpeed;
        if (kb.sKey.isPressed) forward -= driveSpeed;
        if (kb.aKey.isPressed) turn -= turnSpeed;
        if (kb.dKey.isPressed) turn += turnSpeed;

        // Move the base directly via teleportation since it's immovable
        if (rootBody != null)
        {
            rootBody.TeleportRoot(
                rootBody.transform.position + rootBody.transform.forward * forward * Time.deltaTime,
                rootBody.transform.rotation * Quaternion.Euler(0f, turn * Mathf.Rad2Deg * Time.deltaTime, 0f)
            );
        }
    }

    private void HandleLift(Keyboard kb)
    {
        float input = 0f;
        if (kb.rKey.isPressed) input += liftSpeed;
        if (kb.fKey.isPressed) input -= liftSpeed;
        SetPrismaticTarget(lift, input);
    }

    private void HandleArm(Keyboard kb)
    {
        float input = 0f;
        if (kb.tKey.isPressed) input += armSpeed;
        if (kb.gKey.isPressed) input -= armSpeed;

        foreach (var seg in armSegments)
        {
            if (seg != null)
                SetPrismaticTarget(seg, input);
        }
    }

    private void HandleWrist(Keyboard kb)
    {
        if (wristYaw != null)
        {
            float yawInput = 0f;
            if (kb.zKey.isPressed) yawInput += wristYawSpeed;
            if (kb.xKey.isPressed) yawInput -= wristYawSpeed;
            SetRevoluteTarget(wristYaw, yawInput);
        }

        if (wristPitch != null)
        {
            float pitchInput = 0f;
            if (kb.uKey.isPressed) pitchInput += wristPitchSpeed;
            if (kb.jKey.isPressed) pitchInput -= wristPitchSpeed;
            SetRevoluteTarget(wristPitch, pitchInput);
        }

        if (wristRoll != null)
        {
            float rollInput = 0f;
            if (kb.iKey.isPressed) rollInput += wristRollSpeed;
            if (kb.kKey.isPressed) rollInput -= wristRollSpeed;
            SetRevoluteTarget(wristRoll, rollInput);
        }
    }

    private void HandleHead(Keyboard kb)
    {
        if (headPan != null)
        {
            float panInput = 0f;
            if (kb.leftArrowKey.isPressed) panInput += headPanSpeed;
            if (kb.rightArrowKey.isPressed) panInput -= headPanSpeed;
            SetRevoluteTarget(headPan, panInput);
            // headPanAngle = Mathf.Clamp(headPanAngle + panInput * Time.deltaTime * Mathf.Rad2Deg, -223f, 86f);
            // headPan.transform.localRotation = Quaternion.Euler(0f, 0f, headPanAngle);
        }

        if (headTilt != null)
        {
            float tiltInput = 0f;
            if (kb.upArrowKey.isPressed) tiltInput += headTiltSpeed;
            if (kb.downArrowKey.isPressed) tiltInput -= headTiltSpeed;
            SetRevoluteTarget(headTilt, tiltInput);
            // headTiltAngle = Mathf.Clamp(headTiltAngle + tiltInput * Time.deltaTime * Mathf.Rad2Deg, -88f, 45f);
            // headTilt.transform.localRotation = Quaternion.Euler(0f, 0f, headTiltAngle);
        }
    }

    private void HandleGripper(Keyboard kb)
    {
        float input = 0f;
        if (kb.oKey.isPressed) input += gripperSpeed;
        if (kb.lKey.isPressed) input -= gripperSpeed;

        SetRevoluteTarget(gripperFingerLeft, input);
        SetRevoluteTarget(gripperFingerRight, input); // Axes already opposite in URDF
    }

    /// <summary>
    /// Moves a prismatic joint target by a delta each frame.
    /// </summary>
    private void SetPrismaticTarget(ArticulationBody joint, float delta)
    {
        if (joint == null) return;

        var drive = joint.xDrive;
        drive.driveType = ArticulationDriveType.Target;
        float target = drive.target + delta * Time.deltaTime;
        target = Mathf.Clamp(target, drive.lowerLimit, drive.upperLimit);
        drive.target = target;
        drive.stiffness = 1000f;
        drive.damping = 100f;
        drive.forceLimit = 100f;
        joint.xDrive = drive;
    }

    /// <summary>
    /// Moves a revolute joint target by a delta each frame (in degrees).
    /// </summary>
    private void SetRevoluteTarget(ArticulationBody joint, float delta)
    {
        if (joint == null) return;

        var drive = joint.xDrive;
        drive.driveType = ArticulationDriveType.Target;
        float target = drive.target + delta * Time.deltaTime;
        target = Mathf.Clamp(target, drive.lowerLimit, drive.upperLimit);
        drive.target = target;
        drive.stiffness = 1000f;
        drive.damping = 100f;
        drive.forceLimit = 100f;
        joint.xDrive = drive;
    }

    private void LogJointLimits()
    {
        void Log(string name, ArticulationBody body)
        {
            if (body == null) return;
            var d = body.xDrive;
            Debug.Log($"StretchController: {name} limits=[{d.lowerLimit}, {d.upperLimit}] target={d.target}");
        }
        Log("lift", lift);
        Log("headPan", headPan);
        Log("headTilt", headTilt);
        Log("wristYaw", wristYaw);
        Log("wristPitch", wristPitch);
        Log("wristRoll", wristRoll);
        Log("gripperFingerLeft", gripperFingerLeft);
        Log("gripperFingerRight", gripperFingerRight);
    }

    private void LogMissingJoints()
    {
        if (leftWheel == null) Debug.LogWarning("StretchController: link_left_wheel not found");
        if (rightWheel == null) Debug.LogWarning("StretchController: link_right_wheel not found");
        if (lift == null) Debug.LogWarning("StretchController: link_lift not found");
        if (wristYaw == null) Debug.LogWarning("StretchController: link_wrist_yaw not found");
        if (wristPitch == null) Debug.LogWarning("StretchController: link_wrist_pitch not found");
        if (wristRoll == null) Debug.LogWarning("StretchController: link_wrist_roll not found");
        if (headPan == null) Debug.LogWarning("StretchController: link_head_pan not found");
        if (headTilt == null) Debug.LogWarning("StretchController: link_head_tilt not found");
        if (gripperFingerLeft == null) Debug.LogWarning("StretchController: link_gripper_finger_left not found");
        if (gripperFingerRight == null) Debug.LogWarning("StretchController: link_gripper_finger_right not found");

        for (int i = 0; i < armSegments.Length; i++)
        {
            if (armSegments[i] == null)
                Debug.LogWarning($"StretchController: arm segment {i} not found");
        }
    }

    private bool showHelp = true;

    void OnGUI()
    {
        float x = 10f;
        float y = Screen.height - 260f;

        // Toggle
        if (GUI.Button(new Rect(x, y, 30, 20), showHelp ? "−" : "?"))
            showHelp = !showHelp;

        if (!showHelp) return;

        y += 25;
        GUI.skin.label.fontSize = 13;
        GUI.color = new Color(1f, 1f, 1f, 0.85f);

        string instructions =
            "<b>Keyboard Controls</b>\n" +
            "W / S         Drive fwd / back\n" +
            "A / D         Turn left / right\n" +
            "R / F          Lift up / down\n" +
            "T / G         Arm extend / retract\n" +
            "Z / X          Wrist yaw L / R\n" +
            "U / J          Wrist pitch up / dn\n" +
            "I / K           Wrist roll CW / CCW\n" +
            "← / →       Head pan L / R\n" +
            "↑ / ↓          Head tilt up / dn\n" +
            "O / L          Gripper open / close";

        GUI.Label(new Rect(x, y, 260, 230), instructions);
        GUI.color = Color.white;
    }
}
