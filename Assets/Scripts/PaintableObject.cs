using UnityEngine;

public sealed class PaintableObject : MonoBehaviour
{
    [SerializeField]
    private DecalPainterPropertiesScriptableObject _painterProps;

    public IDecalPainterProps Props => _painterProps;
}
