using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public sealed class DecalDepthRenderBehaviour : MonoBehaviour
{
    [SerializeField]
    private MeshPaintTester _meshPaint;

    [SerializeField]
    private CustomDecalProjector _decalProjector;

    [SerializeField]
    private int _textureSize = 2048;

    [SerializeField]
    private List<Renderer> _renderers;

    [SerializeField]
    private RawImage _rawImage;

    [SerializeField]
    private Shader _overrideShader;

    private CommandBuffer _command;
    private Material _overrideMaterial;

    private RenderTexture _depthTexture;

    private Matrix4x4 _viewMatrix;
    private Matrix4x4 _projectionMatrix;

    private OverrideRenderer _overrideRenderer;

    private void Start()
    {
        if (_overrideShader == null)
        {
            _overrideShader = Shader.Find("Unlit/Color");
        }

        _overrideMaterial = new Material(_overrideShader);

        _command = new CommandBuffer();
        _command.name = "DecalDepthRenderBehaviour.CommandBuffer";

        _depthTexture = new RenderTexture(
            _textureSize,
            _textureSize,
            32,
            RenderTextureFormat.Depth,
            0
        );

        if (_rawImage != null)
        {
            _rawImage.texture = _depthTexture;
        }

        var nameID = "_MyDepthTexture";

        _meshPaint.DecalPainter.MappingMaterial.SetTexture(
            nameID,
            _depthTexture,
            RenderTextureSubElement.Depth
        );

        _overrideRenderer = new OverrideRenderer(_renderers, _overrideMaterial);
    }

    private void OnDestroy()
    {
        if (_overrideMaterial != null)
        {
            Destroy(_overrideMaterial);
            _overrideMaterial = null;
        }

        if (_command != null)
        {
            _command.Release();
            _command = null;
        }

        if (_depthTexture != null)
        {
            _depthTexture.Release();
            _depthTexture = null;
        }
    }

    private void Update()
    {
        _viewMatrix = _decalProjector.GetViewMatrix();
        _projectionMatrix = _decalProjector.GetProjectionMatrix();

        _command.SetRenderTarget(_depthTexture);
        _command.ClearRenderTarget(true, true, Color.clear);

        _overrideRenderer.UpdateState(_viewMatrix, _projectionMatrix);
        _overrideRenderer.Render(_command);

        Graphics.ExecuteCommandBuffer(_command);
        _command.Clear();

        var mat = _meshPaint.DecalPainter.MappingMaterial;
        SetCameraMatrices(mat, _viewMatrix, _projectionMatrix);
    }

    private static bool IsCameraProjectionMatrixFlipped()
    {
        if (!SystemInfo.graphicsUVStartsAtTop)
            return false;

        return true;
    }

    private static void SetCameraMatrices(
        Material material,
        Matrix4x4 viewMatrix,
        Matrix4x4 projectionMatrix
    )
    {
        // I_VP = (P * V)^(-1)
        // I_VP = (V^(-1)) * (P^(-1))

        material.SetMatrix(DecalShaderPropertyId.viewMatrix, viewMatrix);
        material.SetMatrix(DecalShaderPropertyId.projectionMatrix, projectionMatrix);
        material.SetMatrix(
            DecalShaderPropertyId.viewAndProjectionMatrix,
            projectionMatrix * viewMatrix
        );

        Matrix4x4 gpuProjectionMatrix = GL.GetGPUProjectionMatrix(
            projectionMatrix,
            IsCameraProjectionMatrixFlipped()
        );
        Matrix4x4 viewAndProjectionMatrix = gpuProjectionMatrix * viewMatrix;
        Matrix4x4 inverseViewMatrix = Matrix4x4.Inverse(viewMatrix);
        Matrix4x4 inverseProjectionMatrix = Matrix4x4.Inverse(gpuProjectionMatrix);
        Matrix4x4 inverseViewProjection = inverseViewMatrix * inverseProjectionMatrix;

        material.SetMatrix(DecalShaderPropertyId.inverseViewMatrix, inverseViewMatrix);
        material.SetMatrix(DecalShaderPropertyId.inverseProjectionMatrix, inverseProjectionMatrix);
        material.SetMatrix(
            DecalShaderPropertyId.inverseViewAndProjectionMatrix,
            inverseViewProjection
        );

        Matrix4x4 worldToCameraMatrix =
            Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f)) * viewMatrix;
        Matrix4x4 cameraToWorldMatrix = worldToCameraMatrix.inverse;
        material.SetMatrix(DecalShaderPropertyId.worldToCameraMatrix, worldToCameraMatrix);
        material.SetMatrix(DecalShaderPropertyId.cameraToWorldMatrix, cameraToWorldMatrix);
    }
}
