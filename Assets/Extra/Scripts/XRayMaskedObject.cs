using System;
using System.Collections;
using UnityEngine;

public sealed class XRayMaskedObject : MonoBehaviour
{
#if UNITY_EDITOR
    public static readonly string ArrayPropertyName = nameof(_imageUrls);
#endif

    [SerializeField]
    private string[] _imageUrls;

    [SerializeField]
    private float _worldUnits = 1;

    [Range(0f, 1f)]
    [SerializeField]
    private float _matchWidthOrHeight;

    private MeshRenderer _meshRenderer;
    private Material _material;
    private Texture2D _texture0;
    private Texture2D _texture1;

    private void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    private void OnDestroy()
    {
        if (_material != null)
        {
            Destroy(_material);
            _material = null;
        }

        if (_texture0 != null)
        {
            Destroy(_texture0);
            _texture0 = null;
        }

        if (_texture1 != null)
        {
            Destroy(_texture1);
            _texture1 = null;
        }
    }

    private IEnumerator Start()
    {
        yield return CustomTextureUtils.ResolveTexture(_imageUrls[0], CallbackTexture0);
        yield return CustomTextureUtils.ResolveTexture(_imageUrls[1], CallbackTexture1);

        if (_texture0 == null || _texture1 == null)
        {
            yield break;
        }

        UpdateMaterial();

        UpdateScale();
    }

    private void CallbackTexture0(Texture2D texture)
    {
        _texture0 = texture;
    }

    private void CallbackTexture1(Texture2D texture)
    {
        _texture1 = texture;
    }

    private void UpdateMaterial()
    {
        _material = _meshRenderer.material;
        _material.SetTexture("_Tex0", _texture0);
        _material.SetTexture("_Tex1", _texture1);
    }

    private void UpdateScale()
    {
        var widthScale = new Vector2(_worldUnits, 0);
        widthScale.y = CustomTextureUtils.CalculateHeight(_texture0, widthScale.x);

        var heightScale = new Vector2(0, _worldUnits);
        heightScale.x = CustomTextureUtils.CalculateWidth(_texture0, heightScale.y);

        var scale = Vector2.Lerp(widthScale, heightScale, _matchWidthOrHeight);

        transform.localScale = new Vector3(scale.x, scale.y, 1);
    }
}
