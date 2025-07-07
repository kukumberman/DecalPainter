using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class TexturePreviewEditorWindow : EditorWindow
{
    public static TexturePreviewEditorWindow Instance;

    private VisualElement _imageElement;
    private Label _label;
    private Rect _windowPosition;

    private Texture2D _texture;

    public Texture2D Texture
    {
        get => _texture;
        set
        {
            if (_texture != value)
            {
                DestroyCurrentTextureIfExists();
            }

            _texture = value;

            AssignTexture();
        }
    }

    private void OnEnable()
    {
        Instance = this;
    }

    private void OnDisable()
    {
        Instance = null;

        DestroyCurrentTextureIfExists();
    }

    private void CreateGUI()
    {
        var el = new VisualElement();
        el.style.position = Position.Absolute;

        _imageElement = new VisualElement();

        el.Add(_imageElement);

        _label = new Label("position");

        rootVisualElement.Add(el);
        rootVisualElement.Add(_label);

        AssignTexture();
    }

    private void OnGUI()
    {
        _windowPosition = position;
    }

    private void Update()
    {
        if (_texture != null)
        {
            var rect = BackgroundSize.Calculate(
                BackgroundSize.Mode.Contain,
                new Vector2(_texture.width, _texture.height),
                _windowPosition.size
            );

            var width = rect.width;
            var height = rect.height;

            _imageElement.style.width = new StyleLength(new Length(width, LengthUnit.Pixel));
            _imageElement.style.height = new StyleLength(new Length(height, LengthUnit.Pixel));

            _imageElement.style.top = new StyleLength(new Length(rect.y, LengthUnit.Pixel));
            _imageElement.style.left = new StyleLength(new Length(rect.x, LengthUnit.Pixel));
        }

        _label.text = _windowPosition.ToString();
    }

    private void DestroyCurrentTextureIfExists()
    {
        if (_texture != null)
        {
            DestroyImmediate(_texture);
            _texture = null;
        }
    }

    private void AssignTexture()
    {
        if (_imageElement == null)
        {
            return;
        }

        _imageElement.style.backgroundImage = new StyleBackground(_texture);
    }
}
