using UnityEngine;

public sealed class NullTextureStorage : ITextureStorage
{
    public Texture2D GetTexture(string key)
    {
        return null;
    }

    public void SetTexture(string key, Texture2D texture)
    {
        // do nothing
    }
}
