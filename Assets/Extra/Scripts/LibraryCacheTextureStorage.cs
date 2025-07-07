using System.IO;
using UnityEngine;

public sealed class LibraryCacheTextureStorage : ITextureStorage
{
    public Texture2D GetTexture(string name)
    {
        EnsureDirectoryExists();

        var filePath = Path.Combine(GetDirectory(), name);

        if (!File.Exists(filePath))
        {
            return null;
        }

        var bytes = File.ReadAllBytes(filePath);

        var texture = new Texture2D(1, 1);

        if (texture.LoadImage(bytes))
        {
            texture.name = name;

            return texture;
        }

        return null;
    }

    public void SetTexture(string name, Texture2D texture)
    {
        EnsureDirectoryExists();

        var path = Path.Combine(GetDirectory(), name);
        var bytes = texture.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
    }

    private static string GetDirectory()
    {
        return Path.GetFullPath(Path.Combine("Library", "CustomDownloadCache", "Images"));
    }

    private static void EnsureDirectoryExists()
    {
        var directory = GetDirectory();

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
