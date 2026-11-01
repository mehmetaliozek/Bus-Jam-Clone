using UnityEngine;

[CreateAssetMenu(fileName = "FrozenPassengerType", menuName = "Bus Jam/Passenger Types/Frozen Type")]
public class FrozenPassengerTypeSO : PassengerTypeSO
{
    [Header("Frozen Settings")]
    [SerializeField] private Color _frozenTint = new(0.65f, 0.92f, 1f, 1f);
    [SerializeField, Range(0f, 1f)] private float _tintStrength = 0.55f;

    public Color FrozenTint => _frozenTint;
    public float TintStrength => _tintStrength;

    private void Reset()
    {
        ConfigurePreset(
            displayName: "Frozen",
            shortTag: "ICE",
            tagColor: new Color(0.35f, 0.85f, 1f, 1f),
            seatRequirement: 1,
            scaleMultiplier: 1f,
            hasParameter: true,
            parameterLabel: "Freeze Moves",
            defaultParameter: 3
        );
    }

    public override PassengerMechanic CreateMechanic() => new FrozenPassengerMechanic(this);

    private sealed class FrozenPassengerMechanic : PassengerMechanic
    {
        private readonly FrozenPassengerTypeSO _type;
        private int _remainingMoves;

        public FrozenPassengerMechanic(FrozenPassengerTypeSO type)
        {
            _type = type;
        }

        protected override void OnInitialize()
        {
            int rawParam = Data != null ? Data.Parameter : 0;
            _remainingMoves = _type.ClampParameter(rawParam);
            RefreshFrozenState();
        }

        public override bool CanMoveFromGrid(bool canReachExit)
        {
            return _remainingMoves <= 0 && canReachExit;
        }

        public override Color GetVisualColor(GameColor baseColor)
        {
            Color baseUnityColor = baseColor.ToUnityColor();
            return _remainingMoves > 0
                ? Color.Lerp(baseUnityColor, _type.FrozenTint, _type.TintStrength)
                : baseUnityColor;
        }

        public override void OnAnyPassengerMovedFromGrid(Passenger movedPassenger)
        {
            if (_remainingMoves <= 0 || Passenger == null || Passenger.State != PassengerState.OnGrid) return;

            _remainingMoves--;
            RefreshFrozenState();
        }

        public override void OnMoveStarted()
        {
            Passenger.SetStatusText(null);
        }

        protected override void OnReset()
        {
            _remainingMoves = 0;
        }

        private void RefreshFrozenState()
        {
            if (Passenger == null) return;

            if (_remainingMoves > 0)
            {
                Passenger.SetStatusText(_remainingMoves.ToString(), Color.white);
            }
            else
            {
                Passenger.SetStatusText(null);
            }

            Passenger.RefreshVisuals();
        }
    }
}

