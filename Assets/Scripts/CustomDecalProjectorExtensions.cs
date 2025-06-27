using UnityEngine;

public static class CustomDecalProjectorExtensions
{
    public static Matrix4x4 GetProjectionMatrix(this CustomDecalProjector decalProjector)
    {
        var size = new Vector2(decalProjector.Width, decalProjector.Height);
        var halfSize = size * 0.5f;

        var ortho = Matrix4x4.Ortho(
            -halfSize.x,
            halfSize.x,
            -halfSize.y,
            halfSize.y,
            decalProjector.NearClipPlane,
            decalProjector.ProjectionDepth
        );

        return ortho;
    }

    public static Matrix4x4 GetViewMatrix(this CustomDecalProjector decalProjector)
    {
        // https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Rendering.CommandBuffer.SetViewMatrix.html

        var decalTransform = decalProjector.transform;

        var origin = decalTransform.position;

        var lookMatrix = Matrix4x4.LookAt(
            origin,
            origin + decalTransform.forward,
            decalTransform.up
        );

        var scaleMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));
        var viewMatrix = scaleMatrix * lookMatrix.inverse;

        return viewMatrix;
    }
}
