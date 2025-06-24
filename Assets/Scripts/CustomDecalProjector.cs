using UnityEngine;

public sealed class CustomDecalProjector : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        var size = transform.lossyScale;

        var center = size.z * 0.5f * Vector3.forward;

        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(center, size);
    }
}
