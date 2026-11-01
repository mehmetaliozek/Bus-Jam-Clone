using UnityEngine;

public abstract class PassengerMechanic
{
    public Passenger Passenger { get; private set; }
    public PassengerData Data { get; private set; }

    public void Initialize(Passenger passenger, PassengerData data)
    {
        Passenger = passenger;
        Data = data;
        OnInitialize();
    }

    public void Reset()
    {
        OnReset();
        Passenger = null;
        Data = null;
    }

    protected virtual void OnInitialize() { }
    protected virtual void OnReset() { }

    public virtual bool CanMoveFromGrid(bool canReachExit) => canReachExit;
    public virtual Color GetVisualColor(GameColor baseColor) => baseColor.ToUnityColor();

    public virtual void OnReachabilityChanged(bool canReachExit) { }
    public virtual void OnAnyPassengerMovedFromGrid(Passenger movedPassenger) { }
    public virtual void OnTick(float deltaTime) { }

    public virtual void OnMoveStarted() { }
    public virtual void OnArrivedAtWaitingSlot() { }
    public virtual void OnBoarded(Transform seat) { }
}

