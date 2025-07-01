using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class Paintable : MonoBehaviour
{
    [SerializeField]
    Texture _brushTexture;

    [SerializeField]
    bool _initializeOnAwake = true;

    MeshRenderer _meshRenderer;
    MeshFilter _meshFilter;
    DecalPainter _decalPainter;

    bool _initialized;

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }
        _initialized = true;

        _meshRenderer = GetComponent<MeshRenderer>();
        _meshFilter = GetComponent<MeshFilter>();

        // このMesh用デカール累積テクスチャを生成・設定
        var props = new DecalPainterProperties
        {
            UvChannelIndex = 0,
            OverridenTextureSize = new Vector2Int(0, 0),
            SizeMode = BakeTextureDimensionsMode.Default,
            TexturePropertyName = "_BaseMap"
        };
        _decalPainter = new DecalPainter(_meshFilter, _meshRenderer, props);
        _decalPainter.BakeAndAssignBaseTexture();

        // ペイントテクスチャを設定
        _decalPainter.SetDecalTexture(_brushTexture);
    }

    public void Paint(
        Vector3 worldPosition,
        Vector3 normal,
        Vector3 tangent,
        float size,
        Color color
    )
    {
        if (!_initialized)
        {
            Debug.LogWarning("not initialized");
            return;
        }

        var positionOS = transform.InverseTransformPoint(worldPosition);
        var normalOS = transform.InverseTransformDirection(normal);
        var tangentOS = transform.InverseTransformDirection(tangent);
        _decalPainter.SetPointer(
            paintPositionOnObjectSpace: positionOS,
            normal: normalOS,
            tangent: tangentOS,
            decalSize: Vector2.one * size,
            projectionDepth: 1f,
            color: color,
            transformScale: transform.lossyScale
        );
        _decalPainter.Paint(true);
    }

    void Awake()
    {
        if (_initializeOnAwake)
        {
            Initialize();
        }
    }

    void OnDestroy()
    {
        _decalPainter?.Dispose();
        _decalPainter = null;
    }
}
