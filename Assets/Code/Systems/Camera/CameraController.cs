using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    private const float FieldOfView = 17.5f;

    [SerializeField] private GridBuilder _gridBuilder;
    [SerializeField] private WaitingSlots _waitingSlots;
    [SerializeField] private BusQueue _busQueue;
    [SerializeField, Range(10f, 90f)] private float _angle = 65f;
    [SerializeField, Min(1f)] private float _distance = 55f;

    private Camera _camera;
    private Bounds _levelBounds;
    private bool _hasBounds;
    private bool _cameraDirty = true;
    private float _lastAspect = -1f;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        _cameraDirty = true;
        if (Application.isPlaying)
        {
            GameEvents.LevelStateChanged += HandleLevelStateChanged;
        }
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
        {
            GameEvents.LevelStateChanged -= HandleLevelStateChanged;
        }
        _hasBounds = false;
        _cameraDirty = true;
    }

    private void LateUpdate()
    {
        if (_camera == null || (!_hasBounds && _gridBuilder == null)) return;
        if (!_cameraDirty && Mathf.Approximately(_lastAspect, _camera.aspect)) return;
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
        _hasBounds = false;

        if (_gridBuilder != null)
        {
            AddTargetBounds(_gridBuilder.transform);
        }

        if (_waitingSlots != null && _waitingSlots.Slots != null)
        {
            foreach (var slot in _waitingSlots.Slots)
            {
                if (slot != null)
                {
                    AddTargetBounds(slot.transform);
                }
            }
        }

        if (_busQueue != null && _busQueue.ActiveBus != null)
        {
            AddTargetBounds(_busQueue.ActiveBus.transform);
        }

        if (_hasBounds)
        {
            UpdateCamera();
        }
    }

    private void AddTargetBounds(Transform target)
    {
        if (target == null) return;

        bool found = false;

        foreach (var r in target.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer) continue;
            AddBounds(r.bounds);
            found = true;
        }

        if (!found)
        {
            AddBounds(new Bounds(target.position, Vector3.zero));
        }
    }

    private void AddBounds(Bounds b)
    {
        if (!_hasBounds)
        {
            _levelBounds = b;
            _hasBounds = true;
        }
        else
        {
            _levelBounds.Encapsulate(b);
        }
    }

    private void UpdateCamera()
    {
        if (!_hasBounds && _gridBuilder == null) return;
        if (_camera == null) _camera = GetComponent<Camera>();
        if (_camera == null) return;

        _camera.orthographic = false;
        _camera.usePhysicalProperties = false;
        _camera.lensShift = Vector2.zero;
        _camera.fieldOfView = FieldOfView;

        Vector3 focusPos = _hasBounds ? _levelBounds.center : _gridBuilder.transform.position;
        Quaternion rot = Quaternion.Euler(_angle, 0f, 0f);
        Quaternion invRot = Quaternion.Inverse(rot);

        float dist = _distance;
        if (_hasBounds)
        {
            float vTan = Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad);
            float hTan = vTan * Mathf.Max(0.01f, _camera.aspect);
            Vector3 min = _levelBounds.min, max = _levelBounds.max;

            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
                Vector3 pt = invRot * (corner - focusPos);
                dist = Mathf.Max(dist, Mathf.Abs(pt.x) / hTan - pt.z, Mathf.Abs(pt.y) / vTan - pt.z, _camera.nearClipPlane + 0.1f - pt.z);
                _camera.farClipPlane = Mathf.Max(_camera.farClipPlane, pt.z + dist + 1f);
            }
        }

        transform.SetPositionAndRotation(focusPos - rot * Vector3.forward * dist, rot);
        _lastAspect = _camera.aspect;
        _cameraDirty = false;
    }

    private void OnValidate()
    {
        _angle = Mathf.Clamp(_angle, 10f, 90f);
        _distance = Mathf.Max(1f, _distance);
        _cameraDirty = true;
    }
}
