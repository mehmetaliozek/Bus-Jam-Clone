using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [SerializeField] private LevelManager _levelManager;
    [SerializeField] private LevelLoader _levelLoader;

    [SerializeField] private Transform _target;
    [SerializeField] private Vector3 _focusOffset;

    [SerializeField] private float _angle = 65f;

    [Tooltip("Minimum camera distance.")]
    [SerializeField] private float _distance = 55f;

    [Tooltip("Reserved space on each screen edge. X: left/right, Y: top/bottom.")]
    [SerializeField]
    private Vector2 _screenPadding = new Vector2(0.06f, 0.1f);

    private const float FieldOfView = 20f;

    private Camera _camera;
    private Bounds _levelBounds;
    private bool _hasBounds;

    private Vector3[] _boundsCorners = new Vector3[8];

    private void OnEnable()
    {
        _camera = GetComponent<Camera>();

        if (!Application.isPlaying || _levelManager == null) return;

        _levelManager.StateChanged += HandleLevelStateChanged;

        if (_levelManager.State == LevelState.Ready || _levelManager.State == LevelState.Playing)
        {
            FrameLevel();
        }
    }

    private void OnDisable()
    {
        if (_levelManager != null)
        {
            _levelManager.StateChanged -= HandleLevelStateChanged;
        }

        _hasBounds = false;
    }

    private void LateUpdate()
    {
        UpdateCamera();
    }

    private void HandleLevelStateChanged(LevelState state)
    {
        if (state == LevelState.Loading)
        {
            _hasBounds = false;
        }
        else if (state == LevelState.Ready)
        {
            FrameLevel();
        }
    }

    [ContextMenu("Frame Level")]
    public void FrameLevel()
    {
        if (_levelLoader == null) return;

        _hasBounds = false;

        if (_levelLoader.GridBuilder != null)
        {
            AddTargetBounds(_levelLoader.GridBuilder.transform);
        }

        if (_levelLoader.PassengerSpawner != null)
        {
            AddTargetBounds(_levelLoader.PassengerSpawner.transform);
        }

        AddWaitingSlotBounds();
        AddActiveBusBounds();

        if (!_hasBounds) return;

        UpdateBoundsCorners();
        UpdateCamera();
    }

    private void AddWaitingSlotBounds()
    {
        WaitingSlots waitingSlots = _levelLoader.WaitingSlots;

        if (waitingSlots == null || waitingSlots.Slots == null) return;

        for (int i = 0; i < waitingSlots.Slots.Count; i++)
        {
            GridCell slot = waitingSlots.Slots[i];

            if (slot == null) continue;

            AddTargetBounds(slot.transform);
        }
    }

    private void AddActiveBusBounds()
    {
        BusQueue busQueue = _levelLoader.BusQueue;

        if (busQueue == null || busQueue.ActiveBus == null) return;

        AddTargetBounds(busQueue.ActiveBus.transform);
    }

    private void AddTargetBounds(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();

        bool hasRenderer = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (!renderer.enabled) continue;
            if (!renderer.gameObject.activeInHierarchy) continue;

            AddBounds(renderer.bounds);
            hasRenderer = true;
        }

        if (!hasRenderer)
        {
            AddBounds(new Bounds(target.position, Vector3.zero));
        }
    }

    private void AddBounds(Bounds bounds)
    {
        if (!_hasBounds)
        {
            _levelBounds = bounds;
            _hasBounds = true;
            return;
        }

        _levelBounds.Encapsulate(bounds);
    }

    private void UpdateBoundsCorners()
    {
        Vector3 min = _levelBounds.min;
        Vector3 max = _levelBounds.max;

        _boundsCorners[0] = new Vector3(min.x, min.y, min.z);
        _boundsCorners[1] = new Vector3(max.x, min.y, min.z);
        _boundsCorners[2] = new Vector3(min.x, max.y, min.z);
        _boundsCorners[3] = new Vector3(max.x, max.y, min.z);

        _boundsCorners[4] = new Vector3(min.x, min.y, max.z);
        _boundsCorners[5] = new Vector3(max.x, min.y, max.z);
        _boundsCorners[6] = new Vector3(min.x, max.y, max.z);
        _boundsCorners[7] = new Vector3(max.x, max.y, max.z);
    }

    private void UpdateCamera()
    {
        if (!_hasBounds && _target == null) return;

        if (_camera == null)
        {
            _camera = GetComponent<Camera>();
        }

        _camera.orthographic = false;
        _camera.usePhysicalProperties = false;
        _camera.lensShift = Vector2.zero;
        _camera.fieldOfView = FieldOfView;

        Vector3 focusPosition = _hasBounds ? _levelBounds.center : _target.position;

        focusPosition += _focusOffset;

        Quaternion rotation = Quaternion.Euler(_angle, 0f, 0f);

        float distance = _distance;

        if (_hasBounds)
        {
            distance = CalculateDistance(focusPosition, rotation);
            UpdateFarClipPlane(focusPosition, rotation, distance);
        }

        Vector3 position = focusPosition - rotation * Vector3.forward * distance;

        transform.SetPositionAndRotation(position, rotation);
    }

    private float CalculateDistance(Vector3 focusPosition, Quaternion rotation)
    {
        Quaternion inverseRotation = Quaternion.Inverse(rotation);

        float verticalTangent = Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad);

        float horizontalTangent = verticalTangent * Mathf.Max(0.01f, _camera.aspect);

        float horizontalSpace = horizontalTangent * (1f - _screenPadding.x * 2f);

        float verticalSpace = verticalTangent * (1f - _screenPadding.y * 2f);

        float distance = _distance;

        for (int i = 0; i < _boundsCorners.Length; i++)
        {
            Vector3 point = inverseRotation * (_boundsCorners[i] - focusPosition);

            float horizontalDistance = Mathf.Abs(point.x) / horizontalSpace - point.z;

            float verticalDistance = Mathf.Abs(point.y) / verticalSpace - point.z;

            float nearClipDistance = _camera.nearClipPlane + 0.1f - point.z;

            distance = Mathf.Max(distance, horizontalDistance);
            distance = Mathf.Max(distance, verticalDistance);
            distance = Mathf.Max(distance, nearClipDistance);
        }

        return distance;
    }

    private void UpdateFarClipPlane(Vector3 focusPosition, Quaternion rotation, float distance)
    {
        Quaternion inverseRotation = Quaternion.Inverse(rotation);

        float farClipPlane = _camera.farClipPlane;

        for (int i = 0; i < _boundsCorners.Length; i++)
        {
            Vector3 point = inverseRotation * (_boundsCorners[i] - focusPosition);

            farClipPlane = Mathf.Max(farClipPlane, point.z + distance + 1f);
        }

        _camera.farClipPlane = farClipPlane;
    }

    private void OnValidate()
    {
        _angle = Mathf.Clamp(_angle, 10f, 90f);
        _distance = Mathf.Max(1f, _distance);

        _screenPadding.x = Mathf.Clamp(_screenPadding.x, 0f, 0.4f);

        _screenPadding.y = Mathf.Clamp(_screenPadding.y, 0f, 0.4f);
    }
}