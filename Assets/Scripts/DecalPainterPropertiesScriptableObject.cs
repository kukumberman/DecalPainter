using UnityEngine;

[CreateAssetMenu]
public sealed class DecalPainterPropertiesScriptableObject : ScriptableObject, IDecalPainterProps
{
    [SerializeField]
    private DecalPainterProperties _props;

    BakeTextureDimensionsMode IDecalPainterProps.SizeMode => _props.SizeMode;
    Vector2Int IDecalPainterProps.OverridenTextureSize => _props.OverridenTextureSize;
    float IDecalPainterProps.SizeMultiplier => _props.SizeMultiplier;
    int IDecalPainterProps.UvChannelIndex => _props.UvChannelIndex;
    string IDecalPainterProps.TexturePropertyName => _props.TexturePropertyName;
}
