using UnityEngine;
using UnityEngine.Rendering;

public sealed class MeshFilterSubmeshBaker : MonoBehaviour
{
    [SerializeField]
    private int _textureSize;

    [SerializeField]
    private Shader _finalShader;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;

    private Mesh _dynamicMesh;

    private CommandBuffer _commandBuffer;
    private Material[] _materialPool;
    public RenderTexture _renderTexture;
    private Material _finalMaterial;

    private void Start()
    {
        var shader = Shader.Find("Custom/BakeSubmesh");

        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

        var sharedMesh = _meshFilter.sharedMesh;
        var sharedMaterials = _meshRenderer.sharedMaterials;

        _materialPool = new Material[sharedMaterials.Length];

        for (int i = 0; i < _materialPool.Length; i++)
        {
            var material = new Material(shader);
            _materialPool[i] = material;
        }

        _dynamicMesh = new Mesh();
        _dynamicMesh.vertices = sharedMesh.vertices;
        _dynamicMesh.normals = sharedMesh.normals;
        _dynamicMesh.triangles = sharedMesh.triangles;
        _dynamicMesh.uv = sharedMesh.uv2;

        _meshFilter.sharedMesh = _dynamicMesh;

        var desc = new RenderTextureDescriptor(
            _textureSize,
            _textureSize,
            RenderTextureFormat.ARGB32,
            0,
            0
        );

        _renderTexture = new RenderTexture(desc);

        _commandBuffer = new CommandBuffer();

        _commandBuffer.SetRenderTarget(_renderTexture);

        for (int i = 0, length = sharedMesh.subMeshCount; i < length; i++)
        {
            var material = _materialPool[i];
            material.mainTexture = sharedMaterials[i].mainTexture;
            _commandBuffer.DrawMesh(sharedMesh, Matrix4x4.identity, _materialPool[i], i, 0);
        }

        Graphics.ExecuteCommandBuffer(_commandBuffer);

        // todo: uv dilate

        _finalMaterial = new Material(_finalShader);
        _finalMaterial.SetTexture("_BaseMap", _renderTexture);

        _meshRenderer.sharedMaterials = new Material[] { _finalMaterial };
    }

    private void OnDestroy()
    {
        if (_commandBuffer != null)
        {
            _commandBuffer.Release();
            _commandBuffer = null;
        }

        if (_materialPool != null)
        {
            foreach (var material in _materialPool)
            {
                Destroy(material);
            }

            _materialPool = null;
        }

        if (_dynamicMesh != null)
        {
            Destroy(_dynamicMesh);
            _dynamicMesh = null;
        }

        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
            _renderTexture = null;
        }

        if (_finalMaterial != null)
        {
            Destroy(_finalMaterial);
            _finalMaterial = null;
        }
    }
}
