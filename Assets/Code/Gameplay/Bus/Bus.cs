using System.Collections.Generic;
using UnityEngine;

public class Bus : MonoBehaviour
{
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static MaterialPropertyBlock _mpb;
    private static Mesh _sharedSmokeMesh;

    [SerializeField] private Transform _boardingPoint;
    [SerializeField] private Transform[] _seats;
    [SerializeField] private Renderer _renderer;
    [SerializeField] private ParticleSystem _exhaustSmoke;
    [SerializeField] private Mesh _smokeMesh;

    private readonly int[] _colorMaterialIndices = { 0, 1 };
    private readonly Dictionary<Passenger, Transform> _seatReservations = new();
    private readonly Dictionary<Transform, Passenger> _reservedSeatOwners = new();
    private readonly HashSet<Passenger> _seatedPassengers = new();
    private int _occupiedSeatCount;
    private bool _exhaustConfigured;

    public GameColor Color { get; private set; }
    public int PassengerCount => _occupiedSeatCount;
    public bool IsFull => _occupiedSeatCount >= GameConstants.BusCapacity;
    public bool HasBoardingPoint => _boardingPoint != null;
    public Vector3 BoardingPosition => _boardingPoint.position;

    public bool IsConfigured => _boardingPoint != null && _seats != null && _seats.Length == GameConstants.BusCapacity;

    private void Awake()
    {
        EnsureExhaustConfigured();
    }

    public void Initialize(BusData data)
    {
        Color = data.Color;
        _seatReservations.Clear();
        _reservedSeatOwners.Clear();
        _seatedPassengers.Clear();
        _occupiedSeatCount = 0;
        StopExhaust(clearImmediate: true);
        ApplyColor();
    }

    public bool TryReserveSeat(Passenger passenger, out Transform seat)
    {
        seat = null;
        if (passenger == null || !IsConfigured || passenger.Color != Color || passenger.State == PassengerState.Boarded)
            return false;

        if (_seatReservations.TryGetValue(passenger, out seat)) return true;

        int requiredSeats = Mathf.Clamp(passenger.RequiredSeats, 1, GameConstants.BusCapacity);
        int availableCount = 0;

        foreach (Transform candidate in _seats)
        {
            if (candidate != null && !_reservedSeatOwners.ContainsKey(candidate))
            {
                availableCount++;
            }
        }

        if (availableCount < requiredSeats) return false;

        int reservedSoFar = 0;
        foreach (Transform candidate in _seats)
        {
            if (candidate == null || _reservedSeatOwners.ContainsKey(candidate)) continue;

            _reservedSeatOwners[candidate] = passenger;
            if (reservedSoFar == 0)
            {
                seat = candidate;
                _seatReservations[passenger] = candidate;
            }

            reservedSoFar++;
            if (reservedSoFar >= requiredSeats) break;
        }

        return seat != null;
    }

    public bool TryCompleteSeating(Passenger passenger, Transform seat)
    {
        if (passenger == null || seat == null) return false;
        if (!_seatReservations.TryGetValue(passenger, out Transform reserved) || reserved != seat) return false;
        if (!_seatedPassengers.Add(passenger)) return false;

        _occupiedSeatCount += Mathf.Clamp(passenger.RequiredSeats, 1, GameConstants.BusCapacity);
        return true;
    }

    public void ReleaseSeatReservation(Passenger passenger)
    {
        if (passenger == null || _seatedPassengers.Contains(passenger)) return;

        _seatReservations.Remove(passenger);

        if (_seats == null) return;
        foreach (Transform candidate in _seats)
        {
            if (candidate != null && _reservedSeatOwners.TryGetValue(candidate, out Passenger owner) && owner == passenger)
            {
                _reservedSeatOwners.Remove(candidate);
            }
        }
    }

    private void ApplyColor()
    {
        if (_renderer == null) return;

        _mpb ??= new MaterialPropertyBlock();
        Color unityColor = Color.ToUnityColor();
        _mpb.SetColor(ColorId, unityColor);
        _mpb.SetColor(BaseColorId, unityColor);

        foreach (int index in _colorMaterialIndices)
        {
            _renderer.SetPropertyBlock(_mpb, index);
        }
    }

    public void PlayExhaust(bool isDeparture)
    {
        if (_exhaustSmoke == null) return;

        EnsureExhaustConfigured();

        _exhaustSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = _exhaustSmoke.main;
        ParticleSystem.EmissionModule emission = _exhaustSmoke.emission;

        if (isDeparture)
        {
            main.duration = 0.34f;
            emission.rateOverTime = 24f;
            emission.rateOverDistance = 1.8f;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, 6, 9, 1, 0.01f));
        }
        else
        {
            main.duration = 0.26f;
            emission.rateOverTime = 18f;
            emission.rateOverDistance = 1.4f;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, 4, 6, 1, 0.01f));
        }

        _exhaustSmoke.Play(true);
    }

    public void StopExhaust(bool clearImmediate = false)
    {
        if (_exhaustSmoke == null) return;

        ParticleSystemStopBehavior behavior = clearImmediate
            ? ParticleSystemStopBehavior.StopEmittingAndClear
            : ParticleSystemStopBehavior.StopEmitting;

        _exhaustSmoke.Stop(true, behavior);
    }

    public void SetExhaustPaused(bool paused)
    {
        if (_exhaustSmoke == null) return;

        if (paused)
        {
            if (_exhaustSmoke.isPlaying)
            {
                _exhaustSmoke.Pause(true);
            }
        }
        else if (_exhaustSmoke.isPaused)
        {
            _exhaustSmoke.Play(true);
        }
    }

    private void EnsureExhaustConfigured()
    {
        if (_exhaustConfigured || _exhaustSmoke == null) return;
        _exhaustConfigured = true;

        _exhaustSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = _exhaustSmoke.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.34f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.44f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 3.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.15f, 1.85f);
        main.gravityModifier = -0.25f;
        main.maxParticles = 18;

        ParticleSystem.EmissionModule emission = _exhaustSmoke.emission;
        emission.enabled = true;
        emission.rateOverTime = 20f;
        emission.rateOverDistance = 1.5f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6, 9, 1, 0.01f) });

        ParticleSystem.ShapeModule shape = _exhaustSmoke.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 44f;
        shape.radius = 0.48f;
        shape.randomDirectionAmount = 0.36f;
        shape.randomPositionAmount = 0.38f;

        ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = _exhaustSmoke.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-1.5f, -0.3f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.6f, 2.0f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-1.9f, 1.9f);

        ParticleSystem.NoiseModule noise = _exhaustSmoke.noise;
        noise.enabled = false;

        ParticleSystem.LimitVelocityOverLifetimeModule limitVelocity = _exhaustSmoke.limitVelocityOverLifetime;
        limitVelocity.enabled = true;
        limitVelocity.limit = 0.4f;
        limitVelocity.dampen = 0.55f;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = _exhaustSmoke.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new(
            new Keyframe(0.00f, 0.45f, 0f, 4.2f),
            new Keyframe(0.20f, 1.05f, 0.4f, -0.3f),
            new Keyframe(0.55f, 0.90f, -0.8f, -3.2f),
            new Keyframe(1.00f, 0.00f, -4.5f, 0f)
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = _exhaustSmoke.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new UnityEngine.Color(1.00f, 1.00f, 1.00f), 0.00f),
                new GradientColorKey(new UnityEngine.Color(0.94f, 0.96f, 0.98f), 0.45f),
                new GradientColorKey(new UnityEngine.Color(0.84f, 0.88f, 0.93f), 1.00f)
            },
            new[]
            {
                new GradientAlphaKey(1.0f, 0.00f),
                new GradientAlphaKey(1.0f, 0.55f),
                new GradientAlphaKey(0.0f, 1.00f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        if (_exhaustSmoke.TryGetComponent(out ParticleSystemRenderer psRenderer))
        {
            psRenderer.renderMode = ParticleSystemRenderMode.Mesh;
            if (_smokeMesh != null)
            {
                psRenderer.mesh = _smokeMesh;
            }
            else if (psRenderer.mesh == null)
            {
                psRenderer.mesh = GetOrCreateLowPolySphereMesh();
            }
            psRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psRenderer.receiveShadows = false;
            psRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }
    }

    private static Mesh GetOrCreateLowPolySphereMesh()
    {
        if (_sharedSmokeMesh != null) return _sharedSmokeMesh;

        float t = (1f + Mathf.Sqrt(5f)) * 0.5f;

        List<Vector3> unitDirs = new(42)
        {
            new Vector3(-1f,  t, 0f).normalized,
            new Vector3( 1f,  t, 0f).normalized,
            new Vector3(-1f, -t, 0f).normalized,
            new Vector3( 1f, -t, 0f).normalized,
            new Vector3(0f, -1f,  t).normalized,
            new Vector3(0f,  1f,  t).normalized,
            new Vector3(0f, -1f, -t).normalized,
            new Vector3(0f,  1f, -t).normalized,
            new Vector3( t, 0f, -1f).normalized,
            new Vector3( t, 0f,  1f).normalized,
            new Vector3(-t, 0f, -1f).normalized,
            new Vector3(-t, 0f,  1f).normalized
        };

        int[] baseFaces =
        {
            0, 11, 5,   0, 5, 1,    0, 1, 7,    0, 7, 10,   0, 10, 11,
            1, 5, 9,    5, 11, 4,   11, 10, 2,  10, 7, 6,   7, 1, 8,
            3, 9, 4,    3, 4, 2,    3, 2, 6,    3, 6, 8,    3, 8, 9,
            4, 9, 5,    2, 4, 11,   6, 2, 10,   8, 6, 7,    9, 8, 1
        };

        Dictionary<int, int> midpointCache = new(30);

        int GetMidpointIndex(int i0, int i1)
        {
            int min = i0 < i1 ? i0 : i1;
            int max = i0 < i1 ? i1 : i0;
            int key = (min << 8) | max;

            if (midpointCache.TryGetValue(key, out int existing))
            {
                return existing;
            }

            int index = unitDirs.Count;
            unitDirs.Add((unitDirs[i0] + unitDirs[i1]).normalized);
            midpointCache[key] = index;
            return index;
        }

        int[] triangles = new int[240];
        int triOffset = 0;

        for (int i = 0; i < baseFaces.Length; i += 3)
        {
            int a = baseFaces[i];
            int b = baseFaces[i + 1];
            int c = baseFaces[i + 2];

            if (Vector3.Dot(Vector3.Cross(unitDirs[b] - unitDirs[a], unitDirs[c] - unitDirs[a]), unitDirs[a]) < 0f)
            {
                (b, c) = (c, b);
            }

            int ab = GetMidpointIndex(a, b);
            int bc = GetMidpointIndex(b, c);
            int ca = GetMidpointIndex(c, a);

            triangles[triOffset++] = a;  triangles[triOffset++] = ab; triangles[triOffset++] = ca;
            triangles[triOffset++] = b;  triangles[triOffset++] = bc; triangles[triOffset++] = ab;
            triangles[triOffset++] = c;  triangles[triOffset++] = ca; triangles[triOffset++] = bc;
            triangles[triOffset++] = ab; triangles[triOffset++] = bc; triangles[triOffset++] = ca;
        }

        int vertCount = unitDirs.Count;
        Vector3[] vertices = new Vector3[vertCount];
        Vector3[] normals = new Vector3[vertCount];

        for (int i = 0; i < vertCount; i++)
        {
            Vector3 n = unitDirs[i];
            normals[i] = n;
            vertices[i] = n * 0.5f;
        }

        _sharedSmokeMesh = new Mesh
        {
            name = "SM_LowPolySmokePuff",
            hideFlags = HideFlags.HideAndDontSave,
            vertices = vertices,
            normals = normals,
            triangles = triangles
        };
        _sharedSmokeMesh.RecalculateBounds();
        _sharedSmokeMesh.UploadMeshData(markNoLongerReadable: false);

        return _sharedSmokeMesh;
    }

    public void PrepareForPool()
    {
        StopExhaust(clearImmediate: true);

        foreach (Passenger passenger in _seatedPassengers)
        {
            if (passenger != null) passenger.HideAfterDeparture();
        }

        _seatedPassengers.Clear();
        _seatReservations.Clear();
        _reservedSeatOwners.Clear();
        _occupiedSeatCount = 0;
    }
}
