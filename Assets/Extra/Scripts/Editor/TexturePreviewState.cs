using UnityEditor;

[InitializeOnLoad]
public static class TexturePreviewState
{
    private static readonly string s_SessionKey = "TexturePreviewEditorWindow.Texture";

    static TexturePreviewState()
    {
        AssemblyReloadEvents.beforeAssemblyReload -= AssemblyReloadEvents_beforeAssemblyReload;
        AssemblyReloadEvents.beforeAssemblyReload += AssemblyReloadEvents_beforeAssemblyReload;

        AssemblyReloadEvents.afterAssemblyReload -= AssemblyReloadEvents_afterAssemblyReload;
        AssemblyReloadEvents.afterAssemblyReload += AssemblyReloadEvents_afterAssemblyReload;
    }

    private static void AssemblyReloadEvents_beforeAssemblyReload()
    {
        if (TexturePreviewEditorWindow.Instance != null)
        {
            var texture = TexturePreviewEditorWindow.Instance.Texture;

            if (texture != null)
            {
                SessionState.SetString(s_SessionKey, texture.name);
            }
        }
    }

    private static void AssemblyReloadEvents_afterAssemblyReload()
    {
        if (TexturePreviewEditorWindow.Instance != null)
        {
            var textureName = SessionState.GetString(s_SessionKey, string.Empty);

            var texture = CustomTextureUtils.GetLocalByName(textureName);

            TexturePreviewEditorWindow.Instance.Texture = texture;
        }
    }
}
