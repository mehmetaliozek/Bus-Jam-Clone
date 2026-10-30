using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(Passenger))]
public class PassengerOutline : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer _sourceRenderer;
    [SerializeField] private Material _outlineMaterial;

    private Passenger _passenger;
    private SkinnedMeshRenderer _outlineRenderer;

    private int _blendShapeCount;

    private void Awake()
    {
        _passenger = GetComponent<Passenger>();

        if (_sourceRenderer == null || _sourceRenderer.sharedMesh == null || _outlineMaterial == null)
        {
            Debug.LogError("PassengerOutline: Renderer veya material eksik.", this);

            enabled = false;
            return;
        }

        CreateOutlineRenderer();
    }

    private void CreateOutlineRenderer()
    {
        GameObject outlineObject = new GameObject("Outline")
        {
            layer = _sourceRenderer.gameObject.layer
        };

        outlineObject.transform.SetParent(_sourceRenderer.transform, false);

        _outlineRenderer = outlineObject.AddComponent<SkinnedMeshRenderer>();

        _outlineRenderer.enabled = false;

        _outlineRenderer.sharedMesh = _sourceRenderer.sharedMesh;
        _outlineRenderer.bones = _sourceRenderer.bones;
        _outlineRenderer.rootBone = _sourceRenderer.rootBone;

        _outlineRenderer.localBounds = _sourceRenderer.localBounds;
        _outlineRenderer.quality = _sourceRenderer.quality;
        _outlineRenderer.updateWhenOffscreen = _sourceRenderer.updateWhenOffscreen;

        _outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _outlineRenderer.receiveShadows = false;
        _outlineRenderer.lightProbeUsage = LightProbeUsage.Off;
        _outlineRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        int subMeshCount = _sourceRenderer.sharedMesh.subMeshCount;
        Material[] materials = new Material[subMeshCount];

        for (int i = 0; i < materials.Length; i++)
        {
            materials[i] = _outlineMaterial;
        }

        _outlineRenderer.sharedMaterials = materials;

        _blendShapeCount = _sourceRenderer.sharedMesh.blendShapeCount;
    }

    private void LateUpdate()
    {
        if (_outlineRenderer == null || _sourceRenderer == null) return;

        bool visible = _passenger.CanMove && _sourceRenderer.enabled && _sourceRenderer.gameObject.activeInHierarchy;

        if (_outlineRenderer.enabled != visible)
        {
            _outlineRenderer.enabled = visible;
        }

        if (!visible) return;

        for (int i = 0; i < _blendShapeCount; i++)
        {
            _outlineRenderer.SetBlendShapeWeight(i, _sourceRenderer.GetBlendShapeWeight(i));
        }
    }

    private void OnDisable()
    {
        if (_outlineRenderer != null)
        {
            _outlineRenderer.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (_outlineRenderer != null)
        {
            Destroy(_outlineRenderer.gameObject);
        }
    }
}