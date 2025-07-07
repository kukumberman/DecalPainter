using UnityEngine;

public class MeshPaintTester : MonoBehaviour
{
    [SerializeField]
    private DecalDepthRenderBehaviour _decalDepthRenderer;

    [SerializeField]
    private DecalPainterProperties _props;

    [Header("Decal")]
    [SerializeField]
    MeshRenderer _decalPlane;

    [Header("Target")]
    [SerializeField]
    private GameObject _target;

    [Space]
    [SerializeField]
    private KeyCode _key = KeyCode.Mouse0;

    [SerializeField]
    private bool _pauseEditorOnPaint = false;

    MeshFilter _targetMeshFilter;
    MeshRenderer _targetMeshRenderer;

    DecalPainter _decalPainter;

    public DecalPainter DecalPainter => _decalPainter;

    void Awake()
    {
        if (_target == null)
        {
            return;
        }

        _targetMeshFilter = _target.GetComponent<MeshFilter>();
        _targetMeshRenderer = _target.GetComponent<MeshRenderer>();

        // TargetMesh専用のデカール累積テクスチャを生成し、セットする
        _decalPainter = new DecalPainter(_targetMeshRenderer, _props);
        _decalPainter.BakeAndAssignBaseTexture();

        // デカール画像を設定する
        _decalPainter.SetDecalTexture(_decalPlane.sharedMaterial.mainTexture);

        _decalDepthRenderer.SetDepthTextureFor(_decalPainter.MappingMaterial);
    }

    void OnDestroy()
    {
        _decalPainter?.Dispose();
        _decalPainter = null;
    }

    void Update()
    {
        if (Input.GetKeyDown(_key))
        {
            var decalPlaneTransform = _decalPlane.transform;

            // ペイント情報をセットアップ
            var targetMeshTransform = _targetMeshFilter.transform;
            var size = decalPlaneTransform.lossyScale;
            _decalPainter.SetPointer(
                paintPositionOnObjectSpace: targetMeshTransform.InverseTransformPoint(
                    decalPlaneTransform.position
                ),
                normal: targetMeshTransform.InverseTransformDirection(-decalPlaneTransform.forward),
                tangent: targetMeshTransform.InverseTransformDirection(decalPlaneTransform.right),
                decalSize: (Vector2)size,
                projectionDepth: size.z,
                color: Color.white,
                transformScale: targetMeshTransform.lossyScale
            );

            _decalDepthRenderer.SetCameraMatrices(_decalPainter.MappingMaterial);

            // 累積描画
            _decalPainter.Paint(true);

#if UNITY_EDITOR
            if (_pauseEditorOnPaint)
            {
                Debug.Break();
            }
#endif
        }
    }

#if UNITY_EDITOR
    // Inspectorにボタン表示
    [UnityEditor.CustomEditor(typeof(MeshPaintTester))]
    public class MeshPaintTesterEditor : UnityEditor.Editor
    {
        private UnityEditor.SerializedProperty _prop;
        private KeyCode _key;

        private void OnEnable()
        {
            _prop = serializedObject.FindProperty("_key");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            _key = (KeyCode)_prop.intValue;

            UnityEditor.EditorGUILayout.Space(30);
            UnityEditor.EditorGUILayout.HelpBox($"{_key}キーでペイント", UnityEditor.MessageType.Info);
        }
    }
#endif
}
