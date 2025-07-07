using UnityEngine;

public interface ITextureStorage
{
    public Texture2D GetTexture(string key);

    public void SetTexture(string key, Texture2D texture);
}
