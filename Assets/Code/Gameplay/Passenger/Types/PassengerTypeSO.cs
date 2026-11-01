using UnityEngine;

[CreateAssetMenu(fileName = "PassengerType", menuName = "Bus Jam/Passenger Types/Basic Type")]
public class PassengerTypeSO : ScriptableObject
{
    [Header("Identity & Editor")]
    [SerializeField] private string _displayName = "Special";
    [SerializeField] private string _editorShortTag = "SPC";
    [SerializeField] private Color _editorTagColor = new(1f, 0.85f, 0.2f, 1f);

    [Header("Gameplay & Visuals")]
    [SerializeField, Min(1)] private int _seatRequirement = 1;
    [SerializeField] private Passenger _prefabOverride;
    [SerializeField] private GameObject _accessoryPrefab;
    [SerializeField] private Vector3 _accessoryLocalPosition = new(0f, 2f, 0f);
    [SerializeField, Range(0.5f, 2f)] private float _scaleMultiplier = 1f;

    [Header("Custom Parameter (Optional)")]
    [SerializeField] private bool _hasCustomParameter;
    [SerializeField] private string _parameterLabel = "Value";
    [SerializeField] private int _defaultParameter = 1;
    [SerializeField] private int _minParameter = 1;
    [SerializeField] private int _maxParameter = 99;

    public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
    public string EditorShortTag => string.IsNullOrWhiteSpace(_editorShortTag) ? DisplayName : _editorShortTag;
    public Color EditorTagColor => _editorTagColor;

    public int SeatRequirement => Mathf.Clamp(_seatRequirement, 1, GameConstants.BusCapacity);
    public Passenger PrefabOverride => _prefabOverride;
    public GameObject AccessoryPrefab => _accessoryPrefab;
    public Vector3 AccessoryLocalPosition => _accessoryLocalPosition;
    public float ScaleMultiplier => Mathf.Clamp(_scaleMultiplier, 0.5f, 2f);

    public bool HasCustomParameter => _hasCustomParameter;
    public string ParameterLabel => string.IsNullOrWhiteSpace(_parameterLabel) ? "Value" : _parameterLabel;
    public int DefaultParameter => Mathf.Clamp(_defaultParameter, _minParameter, MaxParameter);
    public int MinParameter => _minParameter;
    public int MaxParameter => Mathf.Max(_minParameter, _maxParameter);

    public void ConfigurePreset(string displayName, string shortTag, Color tagColor, int seatRequirement = 1, float scaleMultiplier = 1f, bool hasParameter = false, string parameterLabel = "Value", int defaultParameter = 1)
    {
        _displayName = displayName;
        _editorShortTag = shortTag;
        _editorTagColor = tagColor;
        _seatRequirement = Mathf.Clamp(seatRequirement, 1, GameConstants.BusCapacity);
        _scaleMultiplier = Mathf.Clamp(scaleMultiplier, 0.5f, 2f);
        _hasCustomParameter = hasParameter;
        _parameterLabel = parameterLabel;
        _defaultParameter = defaultParameter;
    }

    public int ClampParameter(int value)
    {
        if (!_hasCustomParameter) return 0;
        return value <= 0 ? DefaultParameter : Mathf.Clamp(value, _minParameter, MaxParameter);
    }

    public virtual string GetEditorCellLabel(int parameter)
    {
        if (_hasCustomParameter)
        {
            int val = ClampParameter(parameter);
            return $"{EditorShortTag}:{val}";
        }

        return EditorShortTag;
    }

    public virtual PassengerMechanic CreateMechanic() => null;
}

