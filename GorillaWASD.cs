using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using GorillaLocomotion;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Animations.Rigging;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GorillaWASD : MonoBehaviour
{
    [Header("WASD SCRIPT MADE BY 1AQN")]
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float shiftKeyMoveSpeed = 25f;
    public float jumpPower = 12f;

    [Header("Flight Settings")]
    public Key flightKey = Key.F;
    public float flightSpeed = 10f;

    [HideInInspector] public float mouseSensitivity = 0.25f;
    [HideInInspector] public bool smoothCamera = false;
    [HideInInspector] public float cameraSmoothSpeed = 0.025f;

    [HideInInspector] public bool acceleration = false;
    [HideInInspector] public float slideSmoothness = 0.15f;

    public bool ExtraSettings = false;

    private GorillaLocomotion.Player GorillaPlayer;
    private Rigidbody playerRigidbody;
    private Vector3 rotationVector = Vector3.zero;
    private Vector3 currentVelocity;
    private bool isFlying = false;
    private bool initialized = false;
    private Transform mainCam;
    private float verticalMomentum = 0f;
    private bool isGrounded = true;

    void Start()
    {
        mainCam = Camera.main.transform;
        StartCoroutine(VRCheckRoutine());
    }

    IEnumerator VRCheckRoutine()
    {
        while (GorillaLocomotion.Player.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        GorillaPlayer = GorillaLocomotion.Player.Instance;
        playerRigidbody = GorillaPlayer.GetComponent<Rigidbody>();

        yield return new WaitForSeconds(1f);

        bool vrDetected = false;
        List<UnityEngine.XR.InputDevice> inputDevices = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, inputDevices);

        if (inputDevices.Count > 0)
        {
            foreach (var device in inputDevices)
            {
                if (device.isValid) vrDetected = true;
            }
        }

        if (vrDetected)
        {
            this.enabled = false;
            yield break;
        }

        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;

        if (Keyboard.current[flightKey].wasPressedThisFrame)
        {
            isFlying = !isFlying;
            playerRigidbody.useGravity = !isFlying;
            if (isFlying)
            {
                playerRigidbody.velocity = Vector3.zero;
                verticalMomentum = 0;
            }
        }

        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            rotationVector.x -= mouseDelta.y * mouseSensitivity;
            rotationVector.y += mouseDelta.x * mouseSensitivity;
            rotationVector.x = Mathf.Clamp(rotationVector.x, -90f, 90f);
        }
    }

    void LateUpdate()
    {
        if (!initialized) return;

        Quaternion targetRotation = Quaternion.Euler(rotationVector);

        if (ExtraSettings && smoothCamera)
        {
            float interpolationFactor = Time.deltaTime / Mathf.Max(cameraSmoothSpeed, 0.001f);
            mainCam.localRotation = Quaternion.Slerp(mainCam.localRotation, targetRotation, interpolationFactor);
        }
        else
        {
            mainCam.localRotation = targetRotation;
        }
    }

    void FixedUpdate()
    {
        if (!initialized) return;

        HandlePhysicsMovement();
        AnchorHandsToChest();
        SyncRigToRigidbody();
        SyncCollidersToRigidbody();
    }

    bool CheckIfGrounded()
    {
        return Physics.CheckSphere(playerRigidbody.position, 0.35f, GorillaPlayer.locomotionEnabledLayers);
    }

    void HandlePhysicsMovement()
    {
        Vector3 forward = mainCam.forward;
        Vector3 right = mainCam.right;

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 inputDirection = Vector3.zero;

        if (Keyboard.current.wKey.isPressed) inputDirection += forward;
        if (Keyboard.current.sKey.isPressed) inputDirection -= forward;
        if (Keyboard.current.aKey.isPressed) inputDirection -= right;
        if (Keyboard.current.dKey.isPressed) inputDirection += right;

        float currentSpeed = Keyboard.current.leftShiftKey.isPressed ? shiftKeyMoveSpeed : moveSpeed;

        if (isFlying)
        {
            currentSpeed = flightSpeed;
            if (Keyboard.current.leftShiftKey.isPressed) currentSpeed = flightSpeed * 2f;
        }

        if (ExtraSettings && acceleration)
        {
            float interpolationFactor = Time.fixedDeltaTime / Mathf.Max(slideSmoothness, 0.001f);
            currentVelocity = Vector3.Lerp(currentVelocity, inputDirection * currentSpeed, interpolationFactor);
        }
        else
        {
            currentVelocity = inputDirection * currentSpeed;
        }

        if (isFlying)
        {
            playerRigidbody.velocity = Vector3.zero;
            Vector3 verticalMove = Vector3.zero;
            if (Keyboard.current.eKey.isPressed) verticalMove = Vector3.up * currentSpeed;
            if (Keyboard.current.qKey.isPressed) verticalMove = Vector3.down * currentSpeed;

            Vector3 finalMove = currentVelocity + verticalMove;
            Vector3 nextPos = playerRigidbody.position + finalMove * Time.fixedDeltaTime;
            playerRigidbody.MovePosition(nextPos);
            GorillaPlayer.transform.position = nextPos;
        }
        else
        {
            isGrounded = CheckIfGrounded();
            Vector3 horizontalMove = currentVelocity;
            horizontalMove.y = 0;

            if (Keyboard.current.spaceKey.isPressed && isGrounded && verticalMomentum <= 0.1f)
            {
                verticalMomentum = jumpPower;
            }

            if (!isGrounded || verticalMomentum > 0)
            {
                verticalMomentum -= 25f * Time.fixedDeltaTime;
            }
            else
            {
                verticalMomentum = 0f;
            }

            Vector3 finalMove = horizontalMove + (Vector3.up * verticalMomentum);
            Vector3 nextPos = playerRigidbody.position + finalMove * Time.fixedDeltaTime;

            playerRigidbody.MovePosition(nextPos);
            GorillaPlayer.transform.position = nextPos;
        }
    }

    void SyncRigToRigidbody()
    {
        if (GorillaTagger.Instance == null || GorillaTagger.Instance.offlineVRRig == null) return;

        Vector3 currentPos = playerRigidbody.position;
        Quaternion currentRot = GorillaPlayer.transform.rotation;

        VRRig offlineRig = GorillaTagger.Instance.offlineVRRig;
        offlineRig.transform.SetPositionAndRotation(currentPos, currentRot);

        MultiParentConstraint multiParent = offlineRig.GetComponentInChildren<MultiParentConstraint>();
        if (multiParent != null)
        {
            var sources = multiParent.data.sourceObjects;
            for (int i = 0; i < sources.Count; i++) sources.SetWeight(i, 0f);
            multiParent.data.sourceObjects = sources;
        }

        if (PhotonNetwork.InRoom && GorillaParent.instance != null)
        {
            foreach (VRRig onlineRig in GorillaParent.instance.vrrigs)
            {
                if (onlineRig != null && !onlineRig.isOfflineVRRig)
                {
                    PhotonView rigView = onlineRig.GetComponent<PhotonView>();
                    if (rigView != null && rigView.IsMine)
                    {
                        onlineRig.transform.SetPositionAndRotation(currentPos, currentRot);
                        MultiParentConstraint onlineMultiParent = onlineRig.GetComponentInChildren<MultiParentConstraint>();
                        if (onlineMultiParent != null)
                        {
                            var onlineSources = onlineMultiParent.data.sourceObjects;
                            for (int i = 0; i < onlineSources.Count; i++) onlineSources.SetWeight(i, 0f);
                            onlineMultiParent.data.sourceObjects = onlineSources;
                        }
                    }
                }
            }
        }
    }

    void AnchorHandsToChest()
    {
        if (GorillaTagger.Instance == null || GorillaTagger.Instance.offlineVRRig == null || GorillaPlayer == null) return;

        Vector3 lookDir = mainCam.forward;
        lookDir.y = 0;
        Quaternion bodyRot = Quaternion.LookRotation(lookDir.normalized);

        Vector3 handPosVec = new Vector3(0.095f, 0.25f, 0.15f);
        Vector3 targetPos = playerRigidbody.position + (bodyRot * handPosVec);

        if (GorillaPlayer.leftHandTransform != null) GorillaPlayer.leftHandTransform.gameObject.transform.SetPositionAndRotation(targetPos, bodyRot);
        if (GorillaPlayer.rightHandTransform != null) GorillaPlayer.rightHandTransform.gameObject.transform.SetPositionAndRotation(targetPos, bodyRot);
    }

    void SyncCollidersToRigidbody()
    {
        if (GorillaPlayer == null || playerRigidbody == null) return;
        if (GorillaPlayer.bodyCollider != null) GorillaPlayer.bodyCollider.transform.position = playerRigidbody.position;
        if (GorillaPlayer.headCollider != null) GorillaPlayer.headCollider.transform.position = playerRigidbody.position + (GorillaPlayer.transform.up * 0.5f);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(GorillaWASD))]
public class GorillaWASDEditor : Editor
{
    SerializedProperty mouseSensitivity;
    SerializedProperty smoothCamera;
    SerializedProperty cameraSmoothSpeed;
    SerializedProperty acceleration;
    SerializedProperty slideSmoothness;
    SerializedProperty extraSettings;

    void OnEnable()
    {
        mouseSensitivity = serializedObject.FindProperty("mouseSensitivity");
        smoothCamera = serializedObject.FindProperty("smoothCamera");
        cameraSmoothSpeed = serializedObject.FindProperty("cameraSmoothSpeed");
        acceleration = serializedObject.FindProperty("acceleration");
        slideSmoothness = serializedObject.FindProperty("slideSmoothness");
        extraSettings = serializedObject.FindProperty("ExtraSettings");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, new string[] { "mouseSensitivity", "smoothCamera", "cameraSmoothSpeed", "acceleration", "slideSmoothness", "ExtraSettings" });

        EditorGUILayout.PropertyField(extraSettings);

        if (extraSettings.boolValue)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Camera Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(mouseSensitivity);
            EditorGUILayout.PropertyField(smoothCamera);
            EditorGUILayout.PropertyField(cameraSmoothSpeed);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Slide Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(acceleration);
            EditorGUILayout.PropertyField(slideSmoothness);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
