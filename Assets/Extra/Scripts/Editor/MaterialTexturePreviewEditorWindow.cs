using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[InitializeOnLoad]
public sealed class MaterialTexturePreviewEditorWindow : EditorWindow
{
    private TwoPaneSplitView _splitView;
    private ListView _leftPane;
    private ScrollView _rightPane;
    private VisualElement _image;

    private List<TextureEntry> _entries;

    private List<TextureEntry> Entries
    {
        get => _entries;
        set
        {
            _entries = value;

            _leftPane.itemsSource = _entries;
            _leftPane.selectedIndex = 0;
        }
    }

    private void CreateGUI()
    {
        _splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);

        _leftPane = new ListView();
        _rightPane = new ScrollView(ScrollViewMode.VerticalAndHorizontal);

        _image = new VisualElement();

        _leftPane.makeItem = () => new Label();
        _leftPane.bindItem = (item, index) =>
        {
            (item as Label).text = _entries[index].PropertyName;
        };
        _leftPane.selectionChanged += LeftPane_selectionChanged;

        _splitView.Add(_leftPane);
        _splitView.Add(_rightPane);

        _rightPane.Add(_image);

        rootVisualElement.Add(_splitView);
    }

    private void LeftPane_selectionChanged(IEnumerable<object> selectedItems)
    {
        var entry = selectedItems.ToArray()[0] as TextureEntry;

        var texture = entry.Value;

        var scaleFactor = 0.5f;

        var size = new Vector2(texture.width, texture.height) * scaleFactor;

        var borderWidth = 2f;
        var borderColor = Color.black;

        _image.style.backgroundImage = new StyleBackground(GetBackground(texture));
        _image.style.width = new StyleLength(new Length(size.x, LengthUnit.Pixel));
        _image.style.height = new StyleLength(new Length(size.y, LengthUnit.Pixel));

        _image.style.borderLeftWidth =
            _image.style.borderRightWidth =
            _image.style.borderTopWidth =
            _image.style.borderBottomWidth =
                borderWidth;

        _image.style.borderLeftColor =
            _image.style.borderRightColor =
            _image.style.borderTopColor =
            _image.style.borderBottomColor =
                borderColor;
    }

    private static Background GetBackground(Texture texture)
    {
        if (texture is Texture2D texture2d)
        {
            return Background.FromTexture2D(texture2d);
        }

        if (texture is RenderTexture renderTexture)
        {
            return Background.FromRenderTexture(renderTexture);
        }

        return new Background();
    }

    static MaterialTexturePreviewEditorWindow()
    {
        EditorApplication.contextualPropertyMenu -= OnPropertyContextMenu;
        EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;
    }

    private static void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
    {
        if (property.propertyType == SerializedPropertyType.ObjectReference)
        {
            var value = property.objectReferenceValue;

            if (value != null && value is Material material)
            {
                var list = GetTexturesFromMaterial(material);

                var content = new GUIContent("MaterialTexturePreviewEditorWindow");

                if (list.Count > 0)
                {
                    menu.AddItem(content, false, MenuFunctionCallback, list);
                }
                else
                {
                    menu.AddDisabledItem(content, false);
                }
            }
        }
    }

    private static void MenuFunctionCallback(object userData)
    {
        var window = GetWindow<MaterialTexturePreviewEditorWindow>();
        window.Entries = (List<TextureEntry>)userData;
    }

    private static List<TextureEntry> GetTexturesFromMaterial(Material material)
    {
        var list = new List<TextureEntry>();

        var shader = material.shader;

        for (int i = 0, length = ShaderUtil.GetPropertyCount(shader); i < length; i++)
        {
            var propType = ShaderUtil.GetPropertyType(shader, i);

            if (propType == ShaderUtil.ShaderPropertyType.TexEnv)
            {
                var propName = ShaderUtil.GetPropertyName(shader, i);
                var texture = material.GetTexture(propName);

                if (texture != null)
                {
                    var entry = new TextureEntry { PropertyName = propName, Value = texture };

                    list.Add(entry);
                }
            }
        }

        return list;
    }

    private sealed class TextureEntry
    {
        public string PropertyName;
        public Texture Value;
    }
}
