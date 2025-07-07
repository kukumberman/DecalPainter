using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(XRayMaskedObject))]
public sealed class XRayMaskedObjectEditor : Editor
{
    private XRayMaskedObject _target;

    private EditorCoroutine _editorCoroutine;

    private void Awake()
    {
        _target = target as XRayMaskedObject;
    }

    private void OnEnable()
    {
        EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;
    }

    private void OnDisable()
    {
        EditorApplication.contextualPropertyMenu -= OnPropertyContextMenu;

        if (_editorCoroutine != null)
        {
            EditorCoroutineUtility.StopCoroutine(_editorCoroutine);
            _editorCoroutine = null;
        }
    }

    private void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
    {
        if (property.serializedObject.targetObject != _target)
        {
            return;
        }

        var propPrefix = string.Format("{0}.Array.data", XRayMaskedObject.ArrayPropertyName);

        if (!property.propertyPath.StartsWith(propPrefix))
        {
            return;
        }

        menu.AddItem(
            new GUIContent("TexturePreviewEditorWindow"),
            false,
            MenuFunc,
            property.boxedValue
        );
    }

    private void MenuFunc(object boxedValue)
    {
        var url = (string)boxedValue;

        _editorCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(
            CustomTextureUtils.ResolveTexture(url, TextureCallback)
        );
    }

    private static void TextureCallback(Texture2D texture)
    {
        var window = EditorWindow.GetWindow<TexturePreviewEditorWindow>();
        window.Texture = texture;
    }
}
