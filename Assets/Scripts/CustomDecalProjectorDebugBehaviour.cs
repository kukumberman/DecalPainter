using UnityEngine;

[ExecuteInEditMode]
public sealed class CustomDecalProjectorDebugBehaviour : MonoBehaviour
{
    [SerializeField]
    private CustomDecalProjector _target;

    private void LateUpdate()
    {
        if (_target != null)
        {
            var size = _target.Size;
            var offset = size.z * 0.5f * _target.transform.forward;

            transform.position = _target.transform.position + offset;
            transform.rotation = _target.transform.rotation;
            transform.localScale = size;
        }
    }
}
