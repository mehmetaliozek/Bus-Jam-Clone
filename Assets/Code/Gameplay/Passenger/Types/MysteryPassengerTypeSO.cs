using UnityEngine;

[CreateAssetMenu(fileName = "MysteryPassengerType", menuName = "Bus Jam/Passenger Types/Mystery Type")]
public class MysteryPassengerTypeSO : PassengerTypeSO
{
    [Header("Mystery Settings")]
    [SerializeField] private Color _hiddenColor = new Color32(45, 55, 72, 255);
    [SerializeField] private string _mysterySymbol = "?";

    public Color HiddenColor => _hiddenColor;
    public string MysterySymbol => string.IsNullOrWhiteSpace(_mysterySymbol) ? "?" : _mysterySymbol;

    private void Reset()
    {
        ConfigurePreset(
            displayName: "Mystery",
            shortTag: "?",
            tagColor: new Color(0.85f, 0.55f, 1f, 1f)
        );
    }

    public override PassengerMechanic CreateMechanic() => new MysteryPassengerMechanic(this);

    private sealed class MysteryPassengerMechanic : PassengerMechanic
    {
        private readonly MysteryPassengerTypeSO _type;
        private bool _isRevealed;

        public MysteryPassengerMechanic(MysteryPassengerTypeSO type)
        {
            _type = type;
        }

        protected override void OnInitialize()
        {
            _isRevealed = false;
            Passenger.SetStatusText(_type.MysterySymbol, Color.white);
        }

        public override Color GetVisualColor(GameColor baseColor)
        {
            return _isRevealed ? baseColor.ToUnityColor() : _type.HiddenColor;
        }

        public override void OnReachabilityChanged(bool canReachExit)
        {
            if (_isRevealed || !canReachExit) return;

            _isRevealed = true;
            Passenger.SetStatusText(null);
            Passenger.RefreshVisuals();
        }

        public override void OnMoveStarted()
        {
            _isRevealed = true;
            Passenger.SetStatusText(null);
            Passenger.RefreshVisuals();
        }

        protected override void OnReset()
        {
            _isRevealed = false;
        }
    }
}

