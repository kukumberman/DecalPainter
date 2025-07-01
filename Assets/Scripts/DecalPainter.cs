using System;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public enum BakeTextureDimensionsMode
{
    Default,
    Override,
    Multiply,
}

[Serializable]
public sealed class DecalPainterProperties : IDecalPainterProps
{
    public BakeTextureDimensionsMode SizeMode;
    public Vector2Int OverridenTextureSize;
    public float SizeMultiplier;
    public int UvChannelIndex;
    public string TexturePropertyName;

    BakeTextureDimensionsMode IDecalPainterProps.SizeMode => SizeMode;
    Vector2Int IDecalPainterProps.OverridenTextureSize => OverridenTextureSize;
    float IDecalPainterProps.SizeMultiplier => SizeMultiplier;
    int IDecalPainterProps.UvChannelIndex => UvChannelIndex;
    string IDecalPainterProps.TexturePropertyName => TexturePropertyName;
}

/// <summary>
/// Meshに対してモデル空間にてブラシ描画をし、その描画結果をUVに展開されたTextureとして更新する。
/// </summary>
public class DecalPainter : IDisposable
{
    static readonly ProfilerMarker s_MarkerBake = new ProfilerMarker(
        ProfilerCategory.Render,
        "DecalPainter.BakeBaseTexture"
    );
    static readonly ProfilerMarker s_MarkerPaint = new ProfilerMarker(
        ProfilerCategory.Render,
        "DecalPainter.Paint"
    );

    const string SHADER_NAME = "DecalMapping";
    static readonly int _decalTextureNameID = Shader.PropertyToID("_DecalTexture");
    static readonly int _accumulateTextureNameID = Shader.PropertyToID("_AccumulateTexture");
    static readonly int _decalPositionOSNameID = Shader.PropertyToID("_DecalPositionOS");
    static readonly int _decalSizeNameID = Shader.PropertyToID("_DecalSize");
    static readonly int _decalNormalNameID = Shader.PropertyToID("_DecalNormal");
    static readonly int _decalTangentNameID = Shader.PropertyToID("_DecalTangent");
    static readonly int _projectionDepthID = Shader.PropertyToID("_ProjectionDepth");
    static readonly int _colorNameID = Shader.PropertyToID("_Color");
    static readonly int _objectScaleNameID = Shader.PropertyToID("_ObjectScale");

    private RenderTexture texture;
    private RenderTexture _accumulateTexture;
    private Material mappingMaterial;

    public Material MappingMaterial => mappingMaterial;

    private CommandBuffer _command;
    private IDecalPainterProps _props;
    private MeshFilter _targetMeshFilter;
    private MeshRenderer _targetMeshRenderer;
    private Mesh _targetMesh;
    private Material _targetMeshMaterial;
    private Texture _baseTexture;

    private bool _commitChanges;

    public DecalPainter(
        MeshFilter targetMeshFilter,
        MeshRenderer targetMeshRenderer,
        IDecalPainterProps props
    )
    {
        _command = new CommandBuffer();
        _targetMeshFilter = targetMeshFilter;
        _targetMeshRenderer = targetMeshRenderer;
        _props = props;

        // TargetMeshのMaterialを複製して使う (参照先マテリアルを変更したくないのでInstantiateしたMaterialをSharedに入れて使う)
        _targetMeshMaterial = _targetMeshRenderer.material;
        _targetMeshRenderer.sharedMaterial = _targetMeshMaterial;

        _baseTexture = _targetMeshMaterial.GetTexture(_props.TexturePropertyName);

        _targetMesh = targetMeshFilter.sharedMesh;

        var textureSize = CalculateBakeTextureDimensions();

        // 累積テクスチャ
        var desc = new RenderTextureDescriptor(
            textureSize.x,
            textureSize.y,
            RenderTextureFormat.ARGB32,
            0,
            0
        );

        texture = new RenderTexture(desc);
        _accumulateTexture = new RenderTexture(desc);

        // 転写用マテリアル
        // NOTE: 動的にシェーダーをFindしているので、ビルド時にはProjectSettings>Graphics>Always Included Shadersに入れておく必要がある。
        var shader = Shader.Find(SHADER_NAME);
        if (shader == null)
        {
            Debug.LogError($"not found {SHADER_NAME} shader");
            return;
        }
        mappingMaterial = new Material(shader);
        mappingMaterial.SetTexture(_accumulateTextureNameID, _accumulateTexture);

        var attribute = GetUvAttributeFromChannelIndex(props.UvChannelIndex);
        var hasUvAttribute = _targetMesh.HasVertexAttribute(attribute);
        if (!hasUvAttribute)
        {
            Debug.LogWarning("HasVertexAttribute is false");
        }

        var keyword = string.Format("UV_CHANNEL_{0}", hasUvAttribute ? props.UvChannelIndex : 0);
        mappingMaterial.EnableKeyword(keyword);
    }

    public void Dispose()
    {
        _command.Release();
        _command = null;

        if (texture != null)
        {
            Object.Destroy(texture);
            texture = null;
        }
        if (_accumulateTexture != null)
        {
            Object.Destroy(_accumulateTexture);
            _accumulateTexture = null;
        }
        if (mappingMaterial != null)
        {
            Object.Destroy(mappingMaterial);
            mappingMaterial = null;
        }
        if (_targetMeshMaterial != null)
        {
            Object.Destroy(_targetMeshMaterial);
            _targetMeshMaterial = null;
        }
    }

    /// <summary>
    /// 描画するデカール画像をセットする
    /// </summary>
    public void SetDecalTexture(Texture decalTexture)
    {
        mappingMaterial.SetTexture(_decalTextureNameID, decalTexture);
    }

    public void BakeAndAssignBaseTexture()
    {
        BakeBaseTexture(_baseTexture);

        _targetMeshMaterial.SetTexture(_props.TexturePropertyName, texture);
    }

    private Vector2Int CalculateBakeTextureDimensions()
    {
        if (_props.SizeMode == BakeTextureDimensionsMode.Override)
        {
            return _props.OverridenTextureSize;
        }

        var defaultSize = new Vector2Int(_baseTexture.width, _baseTexture.height);

        if (_props.SizeMode == BakeTextureDimensionsMode.Default)
        {
            return defaultSize;
        }

        if (_props.SizeMode == BakeTextureDimensionsMode.Multiply)
        {
            return new Vector2Int(
                (int)(defaultSize.x * _props.SizeMultiplier),
                (int)(defaultSize.y * _props.SizeMultiplier)
            );
        }

        throw new ArgumentException();
    }

    /// <summary>
    /// texture(累積テクスチャ)に上書き描画をする
    /// </summary>
    private void BakeBaseTexture(Texture source)
    {
        s_MarkerBake.Begin();

        BakeTextureUsingGPU(source);

        s_MarkerBake.End();
    }

    private void BakeTextureUsingGPU(Texture source)
    {
        _command.Blit(source, _accumulateTexture);

        Graphics.ExecuteCommandBuffer(_command);

        _command.Clear();
    }

    /// <summary>
    /// マッピング用マテリアルにデカール情報をセットする。
    /// 累積テクスチャにデカールテクスチャを重畳してるだけ。
    /// 累積テクスチャに上書きするまで累積はされていかない。
    /// </summary>
    public void SetPointer(
        Vector3 paintPositionOnObjectSpace,
        Vector3 normal,
        Vector3 tangent,
        Vector2 decalSize,
        float projectionDepth,
        Color color,
        Vector3 transformScale
    )
    {
        mappingMaterial.SetVector(_decalPositionOSNameID, paintPositionOnObjectSpace);
        mappingMaterial.SetVector(_decalSizeNameID, decalSize);
        mappingMaterial.SetVector(_decalNormalNameID, normal.normalized);
        mappingMaterial.SetVector(_decalTangentNameID, tangent.normalized);
        mappingMaterial.SetColor(_colorNameID, color);
        mappingMaterial.SetFloat(_projectionDepthID, projectionDepth);
        mappingMaterial.SetVector(_objectScaleNameID, transformScale);
    }

    public void Restore()
    {
        _command.Blit(_accumulateTexture, texture);

        Graphics.ExecuteCommandBuffer(_command);

        _command.Clear();
    }

    /// <summary>
    /// texture(累積テクスチャ)に描画
    /// </summary>
    public void Paint(bool commitChanges)
    {
        _commitChanges = commitChanges;

        s_MarkerPaint.Begin();

        PaintUsingGPU();

        s_MarkerPaint.End();
    }

    private void PaintUsingGPU()
    {
        var temporaryRenderTexture = RenderTexture.GetTemporary(_accumulateTexture.descriptor);

        // same as
        /*
         Matrix4x4.TRS(
            _meshFilter.transform.position,
            _meshFilter.transform.rotation,
            _meshFilter.transform.lossyScale
        )
         */
        var matrix = _targetMeshFilter.transform.localToWorldMatrix;

        _command.SetRenderTarget(temporaryRenderTexture);
        _command.ClearRenderTarget(true, true, Color.clear);
        _command.DrawMesh(_targetMesh, matrix, mappingMaterial, 0, 0);
        _command.Blit(temporaryRenderTexture, texture);

        if (_commitChanges)
        {
            _command.Blit(temporaryRenderTexture, _accumulateTexture);
        }

        Graphics.ExecuteCommandBuffer(_command);

        _command.Clear();

        RenderTexture.ReleaseTemporary(temporaryRenderTexture);
    }

    private static readonly VertexAttribute[] s_TexCoordVertexAttributes = new VertexAttribute[]
    {
        VertexAttribute.TexCoord0,
        VertexAttribute.TexCoord1,
        VertexAttribute.TexCoord2,
        VertexAttribute.TexCoord3,
        VertexAttribute.TexCoord4,
        VertexAttribute.TexCoord5,
        VertexAttribute.TexCoord6,
        VertexAttribute.TexCoord7,
    };

    private static VertexAttribute GetUvAttributeFromChannelIndex(int index)
    {
        return s_TexCoordVertexAttributes[index];
    }
}
