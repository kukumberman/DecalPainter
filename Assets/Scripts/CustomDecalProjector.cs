using UnityEngine;

public sealed class CustomDecalProjector : MonoBehaviour
{
    public float Width => transform.lossyScale.x;
    public float Height => transform.lossyScale.y;
    public float ProjectionDepth => transform.lossyScale.z;
    public Vector3 Size => transform.lossyScale;
    public float NearClipPlane => 0.01f;

    private void OnDrawGizmos()
    {
        var size = transform.lossyScale;

        var center = size.z * 0.5f * Vector3.forward;

        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(center, size);
    }
}
