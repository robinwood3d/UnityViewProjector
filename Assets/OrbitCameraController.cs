/*******************************************************************************
* 作者名称：robin
* 描述：相机运动控制器
******************************************************************************/
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;


public enum OrbitCameraState
{
    Orbiting,
    Transitioning,
    Waiting
}

public enum TransitionType
{
    Smooth,
    Fade,
    FadeInFadeOut,
    Teleport,
    Interp
}

[Serializable]
public struct OrbitCameraSettings
{
    public Vector3 orbitLocation;

    public Vector3 orbitRotation;

    public Vector3 constraintBaseAxis;

    public float minPitchOffset;

    public float maxPitchOffset;

    public float minYawOffset;

    public float maxYawOffset;

    public float zoomDistance;

    public float minZoomDistance;

    public float maxZoomDistance;

    public float focalLength;

    public float minFocalLength;

    public float maxFocalLength;

    public bool useFocalZoom;

    public float maxPanDistance;

    public bool panScreenSpace;

    public TransitionType transitionType;

    public float timelineRate;

    public float horizontalViewOffset;

    public float verticalViewOffset;

    public static OrbitCameraSettings defaultSettings
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            OrbitCameraSettings settings = new OrbitCameraSettings();
            settings.constraintBaseAxis = Vector3.forward;
            settings.minPitchOffset = 0f;
            settings.maxPitchOffset = 89.9f;
            settings.minYawOffset = -180f;
            settings.maxYawOffset = 180f;
            settings.zoomDistance = 20f;
            settings.maxZoomDistance = 200;
            settings.focalLength = 17f;
            settings.minFocalLength = 5f;
            settings.maxFocalLength = 170f;
            settings.maxPanDistance = 99999f;
            settings.transitionType = TransitionType.Smooth;
            settings.timelineRate = 1f;

            return settings;
        }

    }
}

public class OrbitCameraController : MonoBehaviour
{
    Camera orbitCamera;

    Timeline transitionTimeline;

    OrbitCameraState orbitCameraState = OrbitCameraState.Orbiting;

    //用户输入

    Vector2 deltaOrbitInput;

    Vector2 deltaPanInput;

    float deltaZoomInput;

    //相机设置

    public float cameraOrbitRate = 1f;

    public float cameraZoomRate = 1f;

    public float cameraPanRate = 1f;

    public float smoothInterpSpeed = 10f;

    public bool enableAutoFocus = true;


    public OrbitCameraSettings orbitCameraSettings = OrbitCameraSettings.defaultSettings;

    //相机运动数据
    public Vector3 targetOrbitLocation;

    public Vector3 targetOrbitRotation;

    public float targetZoomDistance;

    public float targetFocalLength;

    public Vector2 targetViewOffset;

    Vector3 smoothedOrbitLocation;

    Vector3 smoothedOrbitRotation;

    float smoothedZoomDistance;

    float smoothedFocalLength;

    Vector2 smoothedViewOffset;

    //机位切换数据
    Vector3 transitionStartLocation;

    Vector3 transitionStartRotation;

    float transitionStartFocalLength;

    float transitionStartZoomDistance;

    Vector2 transitionStartViewOffset;

    //使用指针定向运动
    public bool usePointerOrbit = false;

    Vector3 pointerPinLocation;

    float pointerPinZoomDistance;

    Vector3 pointerPinCameraLocation;

    public AnimationCurve timelineCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public class TransitionEvent : UnityEvent { };
    public TransitionEvent onTransitionStart = new TransitionEvent();
    public TransitionEvent onTransitionFinished = new TransitionEvent();

    protected virtual void OnValidate()
    {
        orbitCamera = gameObject.GetComponent<Camera>();
        orbitCamera.usePhysicalProperties = true;
        orbitCameraSettings.transitionType = TransitionType.Teleport;
        StartCameraTransition(orbitCameraSettings);
        UpdateSmoothMovement(0f);
        orbitCameraSettings.transitionType = TransitionType.Smooth;
    }

    // Start is called before the first frame update
    protected virtual void Start()
    {
        Cursor.visible = true;
        //Cursor.lockState = CursorLockMode.None;
        orbitCamera = gameObject.GetComponent<Camera>();
        orbitCamera.usePhysicalProperties = true;

        transitionTimeline = gameObject.AddComponent<Timeline>();
        transitionTimeline.AddTrack("Transition", timelineCurve);
        transitionTimeline.AddUpdateEvent(timelineUpdate);
        transitionTimeline.AddFinishEvent(timelineEnd);

    }

    // Update is called once per frame
    protected virtual void Update()
    {
        PlayerInputTest();

        switch (orbitCameraState)
        {
            case OrbitCameraState.Orbiting:
                {
                    HandleOrbitInput();
                    HandleZoomInput();
                    HandlePanInput();
                }
                break;
        }

        UpdateSmoothMovement(Time.deltaTime);

    }

    protected virtual void PlayerInputTest()
    {
        if (Input.GetMouseButton(1))
        {
            AddOrbitInput(new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y")));

        }
        if (Input.GetMouseButton(2))
        {
            AddPanInput(new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")));
        }

        AddZoomInput(Input.GetAxis("Mouse ScrollWheel"));
    }

    public void AddOrbitInput(Vector2 orbitInput)
    {
        deltaOrbitInput = orbitInput * (cameraOrbitRate * 10f / GetViewportScale());
    }

    public void AddZoomInput(float zoomInput)
    {
        float factor = zoomInput > 0 ? 0.6f : 1f;
        factor *= (GetZoomDistance() + 0.02f);
        factor *= (usePointerOrbit ? -4f : -0.8f);
        factor = orbitCameraSettings.useFocalZoom ? 10f : factor;
        deltaZoomInput = zoomInput * cameraZoomRate * (1f / (GetViewportScale() * GetTanHalfFOV())) * factor;
    }

    public void AddPanInput(Vector2 panInput)
    {
        deltaPanInput = panInput * (GetZoomDistance() + 0.02f) * GetTanHalfFOV() * cameraPanRate * (1f / GetViewportScale()) * -0.08f;
    }

    public void StartCameraTransition(OrbitCameraSettings transitionSettings)
    {
        orbitCameraSettings = transitionSettings;
        orbitCameraState = OrbitCameraState.Transitioning;
        transitionStartLocation = targetOrbitLocation;
        transitionStartRotation = targetOrbitRotation;
        transitionStartFocalLength = targetFocalLength;
        transitionStartZoomDistance = targetZoomDistance;
        transitionStartViewOffset = targetViewOffset;
        onTransitionStart?.Invoke();
        StopTransitionTimeline();
        switch (orbitCameraSettings.transitionType)
        {
            case TransitionType.Smooth:
                {
                    transitionTimeline.PlayFromStart();
                }
                break;

            case TransitionType.Fade:
                {

                }
                break;

            case TransitionType.FadeInFadeOut:
                {

                }
                break;

            case TransitionType.Teleport:
                {
                    smoothedOrbitLocation = targetOrbitLocation = orbitCameraSettings.orbitLocation;
                    smoothedOrbitRotation = targetOrbitRotation = orbitCameraSettings.orbitRotation;
                    smoothedZoomDistance = targetZoomDistance = orbitCameraSettings.zoomDistance;
                    smoothedFocalLength = targetFocalLength = orbitCameraSettings.focalLength;
                    smoothedViewOffset = targetViewOffset = new Vector2(
                        orbitCameraSettings.horizontalViewOffset, orbitCameraSettings.verticalViewOffset);
                    orbitCameraState = OrbitCameraState.Orbiting;
                    onTransitionFinished?.Invoke();
                }
                break;

            case TransitionType.Interp:
                {
                    targetOrbitLocation = orbitCameraSettings.orbitLocation;
                    targetOrbitRotation = orbitCameraSettings.orbitRotation;
                    smoothedOrbitRotation = LerpRotatorWithoutRoll(smoothedOrbitRotation, targetOrbitRotation, 0, true);
                    targetZoomDistance = orbitCameraSettings.zoomDistance;
                    targetFocalLength = orbitCameraSettings.focalLength;
                    targetViewOffset = new Vector2(
                        orbitCameraSettings.horizontalViewOffset, orbitCameraSettings.verticalViewOffset);
                    orbitCameraState = OrbitCameraState.Orbiting;
                    onTransitionFinished?.Invoke();
                }
                break;
        }

    }

    protected void PointerPin(float pointerX, float pointerY)
    {
        if (usePointerOrbit)
        {
            Ray pointerRay = Camera.main.ScreenPointToRay(new Vector3(pointerX, pointerY));
            if (Physics.Raycast(pointerRay, out RaycastHit hit))
            {
                pointerPinLocation = hit.point;
            }
            else
            {
                Plane plane = new Plane(orbitCameraSettings.panScreenSpace ? transform.forward : Vector3.up, pointerPinLocation);
                if (plane.Raycast(pointerRay, out float t))
                {
                    pointerPinLocation = pointerRay.GetPoint(t);
                }
                else
                {
                    return;
                }
            }
            targetOrbitLocation = pointerPinLocation;
            smoothedOrbitLocation = targetOrbitLocation;
            Vector3 localVec = transform.InverseTransformDirection(pointerRay.direction);
            Vector3 localRot = Quaternion.LookRotation(localVec).eulerAngles;

            targetViewOffset.x = NormalizeAxis(localRot.y);
            targetViewOffset.y = NormalizeAxis(localRot.x);
            smoothedViewOffset = targetViewOffset;

            targetZoomDistance = Vector3.Distance(transform.position, targetOrbitLocation) * Vector3.Dot(pointerRay.direction, transform.forward);
            smoothedZoomDistance = targetZoomDistance;
            pointerPinZoomDistance = targetZoomDistance;
            pointerPinCameraLocation = transform.position;
        }
    }

    protected void PointerPan(float pointerX, float pointerY)
    {
        if (usePointerOrbit)
        {
            Ray pointerRay = Camera.main.ScreenPointToRay(new Vector3(pointerX, pointerY));
            if (!orbitCameraSettings.panScreenSpace)
            {

                Plane plane = new Plane(Vector3.up, pointerPinLocation);
                Ray panRay = new Ray(pointerPinCameraLocation, pointerRay.direction);
                if (plane.Raycast(panRay, out float t))
                {
                    Vector3 p1 = panRay.GetPoint(t);
                    Plane forwardPlane = new Plane(transform.forward, pointerPinLocation);
                    Vector3 p2 = forwardPlane.ClosestPointOnPlane(p1);
                    targetZoomDistance = pointerPinZoomDistance + Vector3.Dot(transform.forward, p1 - p2);
                }
            }

            Vector3 localVec = transform.InverseTransformDirection(pointerRay.direction);
            Vector3 localRot = Quaternion.LookRotation(localVec).eulerAngles;

            targetViewOffset.x = NormalizeAxis(localRot.y);
            targetViewOffset.y = NormalizeAxis(localRot.x);

        }
    }

    void timelineUpdate()
    {
        float alpha = transitionTimeline.GetTrackValue("Transition");
        targetOrbitLocation = Vector3.Lerp(transitionStartLocation, orbitCameraSettings.orbitLocation, alpha);
        targetOrbitRotation = LerpRotatorWithoutRoll(transitionStartRotation, orbitCameraSettings.orbitRotation, alpha, true);
        targetZoomDistance = Mathf.Lerp(transitionStartZoomDistance, orbitCameraSettings.zoomDistance, alpha);
        targetFocalLength = Mathf.Lerp(transitionStartFocalLength, orbitCameraSettings.focalLength, alpha);
        targetViewOffset = Vector2.Lerp(transitionStartViewOffset, new Vector2(orbitCameraSettings.horizontalViewOffset, orbitCameraSettings.verticalViewOffset), alpha);

    }
    void timelineEnd()
    {
        orbitCameraState = OrbitCameraState.Orbiting;
        onTransitionFinished?.Invoke();
    }
    bool IsPendingYawInRange(float deltaYaw)
    {
        Vector3 deltaRotator = new Vector3(0, deltaYaw, 0);
        Quaternion pendingQuat = Quaternion.Euler(new Vector3(0, targetOrbitRotation.y, 0)) * Quaternion.Euler(deltaRotator);
        Vector3 pendingDir = pendingQuat * Vector3.forward;
        Vector3 baseDir = new Vector3(orbitCameraSettings.constraintBaseAxis.x, 0, orbitCameraSettings.constraintBaseAxis.z).normalized;
        float pendingYaw = Vector3.SignedAngle(baseDir, pendingDir, Vector3.up);
        return (pendingYaw >= orbitCameraSettings.minYawOffset && pendingYaw <= orbitCameraSettings.maxYawOffset);
    }

    bool IsPendingPitchInRange(float DeltaPitch)
    {
        Vector3 deltaRotator = new Vector3(DeltaPitch, 0, 0);
        Quaternion pendingQuat = Quaternion.Euler(new Vector3(targetOrbitRotation.x, 0, 0)) * Quaternion.Euler(deltaRotator);
        Vector3 pendingDir = pendingQuat * Vector3.forward;
        Quaternion baseQuat = Quaternion.LookRotation(orbitCameraSettings.constraintBaseAxis);
        Vector3 baseRotator = new Vector3(baseQuat.eulerAngles.x, 0, 0);
        Vector3 baseDir = Quaternion.Euler(baseRotator) * Vector3.forward;
        float pendingPitch = Vector3.SignedAngle(baseDir, pendingDir, Vector3.right);
        return (pendingPitch >= orbitCameraSettings.minPitchOffset && pendingPitch <= orbitCameraSettings.maxPitchOffset);
    }

    float GetZoomDistance()
    {
        return smoothedZoomDistance;
    }

    float GetFocalLength()
    {
        return orbitCamera.focalLength;
    }

    void SetFocalLength(float inFocalLength)
    {
        orbitCamera.focalLength = inFocalLength;
    }

    void HandleZoomInput()
    {

        if (orbitCameraSettings.useFocalZoom)
        {
            targetFocalLength = Mathf.Clamp(targetFocalLength + deltaZoomInput, orbitCameraSettings.minFocalLength, orbitCameraSettings.maxFocalLength);
        }
        else
        {
            float clampedZoomDist = Mathf.Clamp(targetZoomDistance + deltaZoomInput, orbitCameraSettings.minZoomDistance, orbitCameraSettings.maxZoomDistance);
            targetZoomDistance = usePointerOrbit ? (targetZoomDistance + deltaZoomInput) : clampedZoomDist;

        }
        deltaZoomInput = 0;
    }

    void HandleOrbitInput()
    {
        float inputYaw = IsPendingYawInRange(deltaOrbitInput.x) ? deltaOrbitInput.x : 0;
        float inputPitch = IsPendingPitchInRange(deltaOrbitInput.y) ? deltaOrbitInput.y : 0;
        targetOrbitRotation.x = targetOrbitRotation.x + inputPitch;
        targetOrbitRotation.x = Mathf.Clamp(targetOrbitRotation.x, -89.9f, 89.9f);
        targetOrbitRotation.y = targetOrbitRotation.y + inputYaw;
        targetOrbitRotation.z = 0;
        deltaOrbitInput = Vector2.zero;
    }

    void HandlePanInput()
    {
        if (!usePointerOrbit)
        {
            Vector3 upVector = orbitCameraSettings.panScreenSpace ? transform.up : Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 positionUnclamped = targetOrbitLocation + upVector * deltaPanInput.y + transform.right * deltaPanInput.x;
            Vector3 clampedOffset = Vector3.ClampMagnitude(positionUnclamped - orbitCameraSettings.orbitLocation, orbitCameraSettings.maxPanDistance);
            targetOrbitLocation = orbitCameraSettings.orbitLocation + clampedOffset;
        }
        deltaPanInput = Vector2.zero;
    }

    void UpdateSmoothMovement(float deltaTime)
    {
        float alpha = 1f;
        if (smoothInterpSpeed > 0)
        {
            alpha = smoothInterpSpeed * deltaTime;
        }

        smoothedOrbitLocation = Vector3.Lerp(smoothedOrbitLocation, targetOrbitLocation, alpha);

        smoothedOrbitRotation = LerpRotatorWithoutRoll(
            smoothedOrbitRotation,
            targetOrbitRotation,
            alpha,
            orbitCameraState == OrbitCameraState.Transitioning
            );

        smoothedFocalLength = Mathf.Lerp(smoothedFocalLength, targetFocalLength, alpha);
        SetFocalLength(smoothedFocalLength);

        smoothedViewOffset = Vector2.Lerp(smoothedViewOffset, targetViewOffset, alpha);

        smoothedZoomDistance = Mathf.Lerp(smoothedZoomDistance, targetZoomDistance, alpha);

        Vector3 localViewVector = Quaternion.Euler(smoothedViewOffset.y, smoothedViewOffset.x, 0) * Vector3.forward;
        Vector3 worldViewVector = Quaternion.Euler(smoothedOrbitRotation) * localViewVector;
        float distance = smoothedZoomDistance / Vector3.Dot(worldViewVector, Quaternion.Euler(smoothedOrbitRotation) * Vector3.forward);
        Vector3 offset = worldViewVector * distance;
        Vector3 cameraPosition = smoothedOrbitLocation - offset;
        transform.position = cameraPosition;
        transform.eulerAngles = smoothedOrbitRotation;

    }

    void UpdateFocusDistance()
    {

    }

    public OrbitCameraState GetCameraState()
    {
        return orbitCameraState;
    }

    Vector3 LerpRotatorWithoutRoll(Vector3 A, Vector3 B, float Alpha, bool shortestPath)
    {
        Vector3 result = Vector3.zero;
        result.x = Mathf.Lerp(A.x, B.x, Alpha);
        if (shortestPath)
        {
            A.x = 0f;
            A.z = 0f;
            B.x = 0f;
            B.z = 0f;
            result.y = Quaternion.Slerp(Quaternion.Euler(A), Quaternion.Euler(B), Alpha).eulerAngles.y;
            return result;
        }
        result.y = Mathf.LerpAngle(A.y, B.y, Alpha);
        return result;
    }

    float NormalizeAxis(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }
        return angle;
    }

    float ClampAxis(float angle)
    {
        angle = angle % 360f;
        if (angle < 0f)
        {
            angle += 360f;
        }
        return angle;
    }
    void SetEnableAutoFocus(bool enabled)
    {

    }

    void StopTransitionTimeline()
    {
        transitionTimeline?.Stop();
    }

    float GetViewportScale()
    {
        return UnityEngine.Device.Screen.dpi * 0.01f;
    }

    float GetTanHalfFOV()
    {
        return Mathf.Tan(orbitCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
    }

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            Gizmos.DrawLine(transform.position, smoothedOrbitLocation);
        }
        else
        {
            Gizmos.DrawLine(transform.position, orbitCameraSettings.orbitLocation);
        }
    }

    /// <summary>
    /// 同步位置
    /// </summary>
    public void SyncStateFromTransform()
    {
        // 1. 获取当前的物理旋转
        Vector3 currentEuler = transform.eulerAngles;

        // 2. 同步旋转数据
        smoothedOrbitRotation = currentEuler;
        targetOrbitRotation = currentEuler;

        // 3. 同步位置数据
        Vector3 lookDir = transform.forward;

        // 这里我们简单地使用当前的 targetZoomDistance 来反推中心点
        smoothedOrbitLocation = transform.position + (lookDir * smoothedZoomDistance);
        targetOrbitLocation = smoothedOrbitLocation;

        // 4. 重置视图偏移，防止切回来时画面跳动
        smoothedViewOffset = Vector2.zero;
        targetViewOffset = Vector2.zero;

        // 5. 确保焦距等其他参数也同步（如果有必要）
        smoothedFocalLength = orbitCamera.focalLength;
    }

    /// <summary>
    ///相机移动到指定位置，并重置锚点，
    /// </summary>
    /// <param name="position">新的目标位置</param>
    /// <param name="isTeleport">true=瞬间传送，false=平滑移动</param>
    public void MoveToPosition(Vector3 position, bool isTeleport = false)
    {
        // 1. 核心：修改 Settings 中的锚点位置
        orbitCameraSettings.orbitLocation = position;

        // 2. 设置控制器的目标位置
        targetOrbitLocation = position;

        // 3. 如果需要瞬间到达（传送），则同步平滑插值变量
        if (isTeleport)
        {
            smoothedOrbitLocation = position;

            // 可选：如果你希望传送后，旋转角度保持不变或重置，可以在这里处理
            // smoothedOrbitRotation = targetOrbitRotation; 
        }
    }
}
