using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class CustomTextureUtils
{
    private static readonly ITextureStorage s_TextureStorage = GetTextureStorage();

    private static ITextureStorage GetTextureStorage()
    {
        if (Application.isEditor)
        {
            return new LibraryCacheTextureStorage();
        }

        return new NullTextureStorage();
    }

    public static IEnumerator ResolveTexture(string url, Action<Texture2D> callback)
    {
        var texture = GetLocalByUrl(url);

        if (texture != null)
        {
            callback(texture);

            yield break;
        }

        yield return DownloadTextureWithCorsFallback(url, callback);
    }

    public static Texture2D GetLocalByName(string name)
    {
        return s_TextureStorage.GetTexture(name);
    }

    private static Texture2D GetLocalByUrl(string url)
    {
        var name = EncodeUrl(url);

        return GetLocalByName(name);
    }

    private static IEnumerator DownloadTextureWithCorsFallback(
        string url,
        Action<Texture2D> callback
    )
    {
        Texture2D texture = null;

        yield return DownloadTexture(url, x => texture = x);

        if (texture != null)
        {
            callback(texture);
        }
        else
        {
            var corsUrl = "https://corsproxy.io/?url=" + url;

            yield return DownloadTexture(corsUrl, callback);
        }
    }

    private static IEnumerator DownloadTexture(string url, Action<Texture2D> callback)
    {
        var request = UnityWebRequestTexture.GetTexture(url);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(request.responseCode);
            Debug.Log(request.result);
            Debug.Log(request.error);

            callback(null);

            yield break;
        }

        var texture = DownloadHandlerTexture.GetContent(request);

        var name = EncodeUrl(url);

        texture.name = name;

        callback(texture);
    }

    private static string EncodeUrl(string url)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(url.Trim()));
    }

    public static float CalculateWidth(Texture2D texture, float height)
    {
        var aspect = (float)texture.width / texture.height;

        var width = aspect * height;

        return width;
    }

    public static float CalculateHeight(Texture2D texture, float width)
    {
        var aspect = (float)texture.width / texture.height;

        var height = width / aspect;

        return height;
    }
}
