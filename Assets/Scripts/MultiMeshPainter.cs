using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class MultiMeshPainter : MonoBehaviour
{
    [SerializeField]
    private DecalDepthRenderBehaviour _decalDepthRenderer;

    [SerializeField]
    private CustomDecalProjector _decalProjector;

    [SerializeField]
    private float _maxDistance;

    [SerializeField]
    private KeyCode _key;

    [SerializeField]
    private bool _keepPaintingWhenKeyIsHeld;

    private Transform _decalTransform;
    private Texture _decalTexture;

    private RaycastHit[] _results = new RaycastHit[10];
    private Ray _ray;
    private int _actualCount;

    private readonly Dictionary<PaintableObject, DecalPainter> _decalMap = new();

    private readonly List<PaintableObject> _paintableObjects = new();
    private readonly List<PaintableObject> _paintedObjectsWithoutCommit = new();
    private readonly CollectionDiffChecker<PaintableObject> _diff = new();

    private bool _commitChanges;

    private void Awake()
    {
        _decalTransform = _decalProjector.transform;
        _decalTexture = _decalProjector.GetComponent<MeshRenderer>().sharedMaterial.mainTexture;
    }

    private void OnDestroy()
    {
        foreach (var painter in _decalMap.Values)
        {
            painter.Dispose();
        }
    }

    private void Update()
    {
        _ray = new Ray(_decalTransform.position, _decalTransform.forward);

        var halfExtents = _decalProjector.Size * 0.5f;
        halfExtents.z = 0.1f * 0.5f;

        _actualCount = Physics.BoxCastNonAlloc(
            _ray.origin,
            halfExtents,
            _ray.direction,
            _results,
            _decalTransform.rotation,
            _maxDistance
        );

        KeepOnlyValidHits();

        FetchPaintableTargets();
        RestorePreviousTargets();

        var hasInput = _keepPaintingWhenKeyIsHeld ? Input.GetKey(_key) : Input.GetKeyDown(_key);

        if (hasInput)
        {
            _commitChanges = true;
            PaintOverTargets();
        }
        else
        {
            _commitChanges = false;
            PaintOverTargets();

            _paintedObjectsWithoutCommit.Clear();
            _paintedObjectsWithoutCommit.AddRange(_paintableObjects);
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            for (int i = 0; i < _actualCount; i++)
            {
                var t01 = (float)i / _results.Length;
                var color = new Color(t01, t01, t01);

                var center = _ray.GetPoint(_results[i].distance);

                Gizmos.color = color;
                Gizmos.DrawSphere(_results[i].point, 0.1f);
            }
        }
    }

    private void FetchPaintableTargets()
    {
        _paintableObjects.Clear();

        for (int i = 0; i < _actualCount; i++)
        {
            var target = _results[i].collider.gameObject;

            if (target.TryGetComponent<PaintableObject>(out var paintable))
            {
                _paintableObjects.Add(paintable);
            }
        }
    }

    private void RestorePreviousTargets()
    {
        _diff.Execute(_paintedObjectsWithoutCommit);

        for (int i = 0; i < _diff.Removed.Count; i++)
        {
            var paintable = _diff.Removed[i];

            var painter = _decalMap[paintable];

            painter.Restore();
        }
    }

    private void PaintOverTargets()
    {
        for (int i = 0; i < _paintableObjects.Count; i++)
        {
            var paintable = _paintableObjects[i];

            PaintTarget(paintable);
        }
    }

    private void PaintTarget(PaintableObject target)
    {
        var painter = GetPainterForObject(target);

        _decalDepthRenderer.SetCameraMatrices(painter.MappingMaterial);

        var targetTransform = target.transform;
        var size = _decalProjector.Size;

        painter.SetPointer(
            paintPositionOnObjectSpace: targetTransform.InverseTransformPoint(
                _decalTransform.position
            ),
            normal: targetTransform.InverseTransformDirection(-_decalTransform.forward),
            tangent: targetTransform.InverseTransformDirection(_decalTransform.right),
            decalSize: (Vector2)size,
            projectionDepth: size.z,
            color: Color.white,
            transformScale: targetTransform.lossyScale
        );

        painter.Paint(_commitChanges);
    }

    private DecalPainter GetPainterForObject(PaintableObject target)
    {
        if (_decalMap.TryGetValue(target, out var existingPainter))
        {
            return existingPainter;
        }
        else
        {
            var meshFilter = target.GetComponent<MeshFilter>();
            var meshRenderer = target.GetComponent<MeshRenderer>();

            var painter = new DecalPainter(meshFilter, meshRenderer, target.Props);

            _decalMap.Add(target, painter);

            painter.SetDecalTexture(_decalTexture);

            painter.BakeAndAssignBaseTexture();

            _decalDepthRenderer.SetDepthTexture(painter.MappingMaterial);

            return painter;
        }
    }

    private void KeepOnlyValidHits()
    {
        if (_actualCount < _results.Length)
        {
            var idx = _actualCount;
            var length = _results.Length - idx;
            Array.Clear(_results, idx, length);
        }

        Array.Sort(_results, s_Comparison);
    }

    private static readonly Comparison<RaycastHit> s_Comparison = Comparer<RaycastHit>
        .Create(CompareByDistanceAscending)
        .Compare;

    private static int CompareByDistanceAscending(RaycastHit lhs, RaycastHit rhs)
    {
        bool lhsValid = lhs.collider != null;
        bool rhsValid = rhs.collider != null;

        if (lhsValid && !rhsValid)
            return -1; // valid hits come before invalid

        if (!lhsValid && rhsValid)
            return 1; // invalid hits go after valid

        if (!lhsValid && !rhsValid)
            return 0; // both invalid, consider equal

        return lhs.distance.CompareTo(rhs.distance);
    }
}
