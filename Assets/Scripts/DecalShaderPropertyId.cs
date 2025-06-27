using UnityEngine;

public static class DecalShaderPropertyId
{
    public static readonly int viewMatrix = ID("UNITY_MATRIX_V"); // unity_MatrixV
    public static readonly int projectionMatrix = ID("glstate_matrix_projection"); // glstate_matrix_projection
    public static readonly int viewAndProjectionMatrix = ID("UNITY_MATRIX_VP"); // unity_MatrixVP

    public static readonly int inverseViewMatrix = ID("UNITY_MATRIX_I_V"); // unity_MatrixInvV
    public static readonly int inverseProjectionMatrix = ID("UNITY_MATRIX_I_P"); // unity_MatrixInvP
    public static readonly int inverseViewAndProjectionMatrix = ID("UNITY_MATRIX_I_VP"); // unity_MatrixInvVP

    public static readonly int cameraProjectionMatrix = ID("unity_CameraProjection"); // unity_CameraProjection
    public static readonly int inverseCameraProjectionMatrix = ID("unity_CameraInvProjection"); // unity_CameraInvProjection
    public static readonly int worldToCameraMatrix = ID("unity_WorldToCamera"); // unity_WorldToCamera
    public static readonly int cameraToWorldMatrix = ID("unity_CameraToWorld"); // unity_CameraToWorld

    public static int ID(string key)
    {
        return Shader.PropertyToID("X_" + key);
    }
}
