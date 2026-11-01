using System;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(PassengerMovement))]
[RequireComponent(typeof(PassengerAnimatorController))]
public class Passenger : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static MaterialPropertyBlock _mpb;

    public event Action<Passenger> Clicked;

    [SerializeField] private Renderer _renderer;
    [SerializeField] private Material _outlineMaterial;
    [SerializeField] private Transform _accessoryAnchor;

    public GameColor Color { get; private set; }
    public Vector2Int Coordinates { get; private set; }
    public PassengerTypeSO Type { get; private set; }
    public PassengerState State { get; private set; }
    public bool CanMove { get; private set; }
    public PassengerMovement Movement { get; private set; }
    public Passenger SourcePrefab { get; set; }

    public int RequiredSeats => Type != null ? Type.SeatRequirement : 1;

    private PassengerAnimatorController _animatorController;
    private Transform _spawnParent;
    private Vector3 _baseLocalScale = Vector3.one;
    private PassengerMechanic _mechanic;
    private GameObject _spawnedAccessory;
    private TextMeshPro _statusText;
    private bool _lastReachable;

    private void Awake()
    {
        Movement = GetComponent<PassengerMovement>();
        _animatorController = GetComponent<PassengerAnimatorController>();
        _baseLocalScale = transform.localScale;
    }

    public void Initialize(PassengerData data)
    {
        ResetMechanicState();

        _spawnParent = transform.parent;

        Color = data.Color;
        Coordinates = data.Coordinates;
        Type = data.Type;
        _lastReachable = false;

        float scaleMultiplier = Type != null ? Type.ScaleMultiplier : 1f;
        transform.localScale = _baseLocalScale * scaleMultiplier;

        SetState(PassengerState.OnGrid);
        SetCanMove(false);

        _animatorController.SetRunning(false);
        _animatorController.SetSitting(false);

        if (Type != null)
        {
            if (Type.AccessoryPrefab != null)
            {
                Transform parent = _accessoryAnchor != null ? _accessoryAnchor : transform;
                _spawnedAccessory = Instantiate(Type.AccessoryPrefab, parent, false);
                _spawnedAccessory.transform.localPosition = _accessoryAnchor != null ? Vector3.zero : Type.AccessoryLocalPosition;
            }

            _mechanic = Type.CreateMechanic();
            _mechanic?.Initialize(this, data);
        }

        RefreshVisuals();
    }

    public void RefreshVisuals()
    {
        ApplyColor();
        SetCanMove(_lastReachable);
    }

    private void ApplyColor()
    {
        if (_renderer == null) return;

        _mpb ??= new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(_mpb, 0);
        Color unityColor = _mechanic != null ? _mechanic.GetVisualColor(Color) : Color.ToUnityColor();
        _mpb.SetColor(BaseColorId, unityColor);
        _mpb.SetColor(ColorId, unityColor);
        _renderer.SetPropertyBlock(_mpb, 0);
    }

    public void SetStatusText(string text, Color? color = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            if (_statusText != null)
            {
                _statusText.gameObject.SetActive(false);
            }
            return;
        }

        if (_statusText == null)
        {
            GameObject labelObj = new("StatusLabel");
            Transform parent = _accessoryAnchor != null ? _accessoryAnchor : transform;
            labelObj.transform.SetParent(parent, false);
            labelObj.transform.localPosition = _accessoryAnchor != null ? Vector3.zero : new Vector3(0f, 1.25f, -0.32f);
            labelObj.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);

            _statusText = labelObj.AddComponent<TextMeshPro>();
            _statusText.alignment = TextAlignmentOptions.Center;
            _statusText.fontSize = 3.6f;
            _statusText.fontStyle = FontStyles.Bold;
            _statusText.enableWordWrapping = false;
            _statusText.raycastTarget = false;
            _statusText.sortingOrder = 20;

            Material mat = _statusText.fontMaterial;
            if (mat != null)
            {
                mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
                mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.35f);
                mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color32(12, 16, 24, 255));

                mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color32(0, 0, 0, 200));
                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.4f);
                mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.2f);
                mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.2f);
            }
        }

        _statusText.text = text;
        _statusText.color = color ?? UnityEngine.Color.white;
        _statusText.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        SetOutline(false);
    }

    private void OnMouseDown()
    {
        NotifyClicked();
    }

    private void NotifyClicked()
    {
        if (!CanMove) return;

        Clicked?.Invoke(this);
    }

    public void UpdateReachability(bool canReachExit)
    {
        _lastReachable = canReachExit;
        if (State == PassengerState.OnGrid)
        {
            _mechanic?.OnReachabilityChanged(canReachExit);
        }
        SetCanMove(canReachExit);
    }

    public void NotifyOtherPassengerMoved(Passenger movedPassenger)
    {
        if (State != PassengerState.OnGrid || movedPassenger == this) return;
        _mechanic?.OnAnyPassengerMovedFromGrid(movedPassenger);
    }

    public void TickMechanic(float deltaTime)
    {
        if (State is PassengerState.OnGrid or PassengerState.Waiting)
        {
            _mechanic?.OnTick(deltaTime);
        }
    }

    public void SetCanMove(bool canMove)
    {
        _lastReachable = canMove;
        bool allowedByMechanic = _mechanic == null || _mechanic.CanMoveFromGrid(canMove);
        bool newCanMove = State == PassengerState.OnGrid && canMove && allowedByMechanic;
        if (CanMove == newCanMove) return;

        CanMove = newCanMove;
        SetOutline(CanMove);
    }

    private void SetOutline(bool show)
    {
        if (_renderer == null || _outlineMaterial == null) return;

        _renderer.sharedMaterials = show ? new[] { _renderer.sharedMaterial, _outlineMaterial } : new[] { _renderer.sharedMaterial };
    }

    private void SetState(PassengerState newState)
    {
        if (State == newState) return;

        State = newState;
    }

    public void BeginMoving()
    {
        SetState(PassengerState.Moving);
        SetCanMove(false);
        _mechanic?.OnMoveStarted();
    }

    public void ArriveAtWaitingSlot()
    {
        SetState(PassengerState.Waiting);
        SetCanMove(false);
        transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        _mechanic?.OnArrivedAtWaitingSlot();
    }

    public void Board(Transform seat)
    {
        SetState(PassengerState.Boarded);
        SetCanMove(false);
        SetStatusText(null);

        transform.SetParent(seat, true);
        transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 90f, 0f));
        _animatorController.SetSitting(true);
        _mechanic?.OnBoarded(seat);
    }

    public void HideAfterDeparture()
    {
        SetStatusText(null);
        gameObject.SetActive(false);

        if (_spawnParent != null && _spawnParent.gameObject.scene.isLoaded)
        {
            transform.SetParent(_spawnParent, false);
        }
    }

    public void ResetMechanicState()
    {
        if (_mechanic != null)
        {
            _mechanic.Reset();
            _mechanic = null;
        }

        if (_spawnedAccessory != null)
        {
            Destroy(_spawnedAccessory);
            _spawnedAccessory = null;
        }

        SetStatusText(null);
        SetOutline(false);
    }
}
