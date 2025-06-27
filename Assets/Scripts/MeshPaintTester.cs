using UnityEngine;

public class MeshPaintTester : MonoBehaviour
{
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
    Material _targetMeshMaterial;

    public DecalPainter DecalPainter => _decalPainter;

    void Awake()
    {
        _targetMeshFilter = _target.GetComponent<MeshFilter>();
        _targetMeshRenderer = _target.GetComponent<MeshRenderer>();

        // TargetMeshのMaterialを複製して使う (参照先マテリアルを変更したくないのでInstantiateしたMaterialをSharedに入れて使う)
        _targetMeshMaterial = _targetMeshRenderer.material;
        _targetMeshRenderer.sharedMaterial = _targetMeshMaterial;

        // TargetMesh専用のデカール累積テクスチャを生成し、セットする
        _decalPainter = new DecalPainter(_targetMeshFilter, _props);
        _decalPainter.BakeBaseTexture(_targetMeshMaterial.mainTexture);
        _targetMeshMaterial.mainTexture = _decalPainter.texture;

        // デカール画像を設定する
        _decalPainter.SetDecalTexture(_decalPlane.sharedMaterial.mainTexture);
    }

    void OnDestroy()
    {
        _decalPainter?.Dispose();
        _decalPainter = null;

        if (_targetMeshMaterial != null)
        {
            Destroy(_targetMeshMaterial);
        }
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

            // 累積描画
            _decalPainter.Paint();

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
