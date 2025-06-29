using UnityEngine;

public interface IDecalPainterProps
{
    BakeTextureDimensionsMode SizeMode { get; }
    Vector2Int OverridenTextureSize { get; }
    float SizeMultiplier { get; }
    int UvChannelIndex { get; }
    string TexturePropertyName { get; }
}
