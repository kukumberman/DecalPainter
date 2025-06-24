using System;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public enum DecalPainterDevice
{
    None,
    CPU,
    GPU,
}

[Serializable]
public sealed class DecalPainterProperties
{
    public bool OverrideTextureSize;
    public int TextureSize;
    public DecalPainterDevice Device;
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

    public Texture texture { get; private set; }
    public Material mappingMaterial { get; private set; }

    private CommandBuffer _command;

    Mesh _targetMesh;

    public DecalPainter(MeshFilter targetMeshFilter, DecalPainterProperties props)
    {
        _command = new CommandBuffer();

        // 転写に使う情報。強制したいのでMeshFilterでもらい、Meshのコピーを複製。
        _targetMesh = targetMeshFilter.mesh;

        var textureSize = props.TextureSize;

        // 累積テクスチャ
        if (props.Device == DecalPainterDevice.GPU)
        {
            texture = new RenderTexture(
                props.TextureSize,
                props.TextureSize,
                0,
                RenderTextureFormat.ARGB32,
                0
            );
        }
        else if (props.Device == DecalPainterDevice.CPU)
        {
            var texture2D = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            var pixels = new Color[textureSize * textureSize];
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = Color.white;
            }
            texture2D.SetPixels(pixels);
            texture2D.Apply();

            texture = texture2D;
        }
        else
        {
            throw new ArgumentException();
        }

        // 転写用マテリアル
        // NOTE: 動的にシェーダーをFindしているので、ビルド時にはProjectSettings>Graphics>Always Included Shadersに入れておく必要がある。
        var shader = Shader.Find(SHADER_NAME);
        if (shader == null)
        {
            Debug.LogError($"not found {SHADER_NAME} shader");
            return;
        }
        mappingMaterial = new Material(shader);
        mappingMaterial.SetTexture(_accumulateTextureNameID, texture);
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
        if (mappingMaterial != null)
        {
            Object.Destroy(mappingMaterial);
            mappingMaterial = null;
        }
        if (_targetMesh != null)
        {
            Object.Destroy(_targetMesh);
            _targetMesh = null;
        }
    }

    /// <summary>
    /// 描画するデカール画像をセットする
    /// </summary>
    public void SetDecalTexture(Texture decalTexture)
    {
        mappingMaterial.SetTexture(_decalTextureNameID, decalTexture);
    }

    /// <summary>
    /// texture(累積テクスチャ)に上書き描画をする
    /// </summary>
    public void BakeBaseTexture(Texture source)
    {
        s_MarkerBake.Begin();

        if (texture is Texture2D)
        {
            BakeTextureUsingCPU(source);
        }
        else if (texture is RenderTexture)
        {
            BakeTextureUsingGPU(source);
        }

        s_MarkerBake.End();
    }

    private void BakeTextureUsingCPU(Texture source)
    {
        if (source == null)
        {
            return;
        }
        var src = source;
        var dst = texture as Texture2D;

        // RenderTargetの設定
        var temporaryActiveRenderTarget = RenderTexture.GetTemporary(dst.width, dst.height, 0);
        var activeRenderTexture = RenderTexture.active;
        RenderTexture.active = temporaryActiveRenderTarget;

        // sourceを描画して、dst(累積テクスチャ)に書き込む
        Graphics.Blit(src, temporaryActiveRenderTarget);
        dst.ReadPixels(new Rect(0f, 0f, dst.width, dst.height), 0, 0);
        dst.Apply();

        // RenderTargetを元に戻す
        RenderTexture.active = activeRenderTexture;
        RenderTexture.ReleaseTemporary(temporaryActiveRenderTarget);
    }

    private void BakeTextureUsingGPU(Texture source)
    {
        _command.Blit(source, texture);

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

    /// <summary>
    /// texture(累積テクスチャ)に描画
    /// </summary>
    public void Paint()
    {
        s_MarkerPaint.Begin();

        if (texture is Texture2D)
        {
            PaintUsingCPU();
        }
        else if (texture is RenderTexture)
        {
            PaintUsingGPU();
        }

        s_MarkerPaint.End();
    }

    private void PaintUsingCPU()
    {
        var dst = texture as Texture2D;

        // RenderTargetの設定
        var temporaryRenderTexture = RenderTexture.GetTemporary(dst.width, dst.height, 0);
        var activeRenderTexture = RenderTexture.active;
        RenderTexture.active = temporaryRenderTexture;

        // 対象Meshを用いて、デカール画像を累積テクスチャに重ねてRenderTargetに描画する
        GL.Clear(clearDepth: true, clearColor: true, Color.clear);
        mappingMaterial.SetPass(0);
        Graphics.DrawMeshNow(_targetMesh, Vector3.zero, Quaternion.identity);

        // RenderTargetを累積テクスチャに書き込む
        dst.ReadPixels(new Rect(0f, 0f, dst.width, dst.height), 0, 0);
        dst.Apply();

        // RenderTargetを元に戻す
        RenderTexture.active = activeRenderTexture;
        RenderTexture.ReleaseTemporary(temporaryRenderTexture);
    }

    private void PaintUsingGPU()
    {
        var temporaryRenderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0);

        _command.SetRenderTarget(temporaryRenderTexture);
        _command.DrawMesh(_targetMesh, Matrix4x4.identity, mappingMaterial, 0, 0);
        _command.Blit(temporaryRenderTexture, texture);

        Graphics.ExecuteCommandBuffer(_command);

        _command.Clear();

        RenderTexture.ReleaseTemporary(temporaryRenderTexture);
    }
}
