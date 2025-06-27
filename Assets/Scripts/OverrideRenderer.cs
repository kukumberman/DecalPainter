using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class OverrideRenderer
{
    private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker(
        ProfilerCategory.Render,
        "OverrideRenderer.Render (ProfilerMarker)"
    );

    private readonly IReadOnlyList<Renderer> _renderers;
    private readonly Material _overrideMaterial;

    private Dictionary<Renderer, int> _renderMap;

    private readonly Plane[] _frustumPlanes = new Plane[6];

    private Matrix4x4 _viewMatrix;
    private Matrix4x4 _projectionMatrix;

    public OverrideRenderer(IReadOnlyList<Renderer> renderers, Material overrideMaterial)
    {
        _renderers = renderers;
        _overrideMaterial = overrideMaterial;

        BuildRenderMap();
    }

    public void UpdateState(Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
    {
        _viewMatrix = viewMatrix;
        _projectionMatrix = projectionMatrix;
    }

    public void Render(CommandBuffer command)
    {
        s_ProfilerMarker.Begin();

        var worldToProjectionMatrix = _projectionMatrix * _viewMatrix;

        GeometryUtility.CalculateFrustumPlanes(worldToProjectionMatrix, _frustumPlanes);

        command.SetViewProjectionMatrices(_viewMatrix, _projectionMatrix);

        for (int i = 0; i < _renderers.Count; i++)
        {
            DrawRenderer(command, _renderers[i]);
        }

        s_ProfilerMarker.End();
    }

    private void BuildRenderMap()
    {
        _renderMap = new Dictionary<Renderer, int>();

        var tempList = new List<Material>();

        for (int i = 0; i < _renderers.Count; i++)
        {
            var renderer = _renderers[i];
            renderer.GetSharedMaterials(tempList);

            _renderMap.Add(renderer, tempList.Count);

            tempList.Clear();
        }
    }

    private bool DrawRenderer(CommandBuffer command, Renderer renderer)
    {
        if (!renderer.enabled)
        {
            return false;
        }

        if (!renderer.gameObject.activeInHierarchy)
        {
            return false;
        }

        var bounds = renderer.bounds;

        if (!GeometryUtility.TestPlanesAABB(_frustumPlanes, bounds))
        {
            return false;
        }

        var subMeshCount = _renderMap[renderer];

        for (int j = 0; j < subMeshCount; j++)
        {
            command.DrawRenderer(renderer, _overrideMaterial, j);
        }

        return true;
    }
}
