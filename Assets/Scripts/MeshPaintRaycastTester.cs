using UnityEngine;

public sealed class MeshPaintRaycastTester : MonoBehaviour
{
    [SerializeField]
    private Transform _positionContainer;

    [SerializeField]
    private Transform _rollContainer;

    [SerializeField]
    private float _hitDistanceOffset;

    [SerializeField]
    private bool _useCameraUpDirection = false;

    [SerializeField]
    private float _angleMultiplier = 1f;

    [SerializeField]
    private float _scaleMultiplier;

    [SerializeField]
    private float _scaleMin = 0.5f;

    [SerializeField]
    private float _scaleMax = 2f;

    private Camera _camera;

    private float _scrollY;
    private float _scrollSign;

    private float _currentScale;

    private void Start()
    {
        _camera = Camera.main;

        _currentScale = _positionContainer.localScale.x;
        SetScale(_currentScale);
    }

    private void Update()
    {
        _scrollY = Input.mouseScrollDelta.y;
        _scrollSign = Mathf.Sign(_scrollY);

        var ray = _camera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out var hit))
        {
            _positionContainer.position = hit.point + hit.normal * _hitDistanceOffset;

            if (_useCameraUpDirection)
            {
                Vector3 forward = -hit.normal;
                Vector3 up = _camera.transform.up;

                _positionContainer.rotation = Quaternion.LookRotation(forward, up);
            }
            else
            {
                _positionContainer.forward = hit.normal * -1f;
            }

            ChangeAngle();

            ChangeScale();
        }
    }

    private void ChangeAngle()
    {
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (_scrollY != 0)
            {
                var eulerAngles = _rollContainer.eulerAngles;

                eulerAngles.z += _scrollSign * _angleMultiplier;

                _rollContainer.eulerAngles = eulerAngles;
            }
        }
    }

    private void ChangeScale()
    {
        if (Input.GetKey(KeyCode.LeftAlt))
        {
            if (_scrollY != 0)
            {
                _currentScale += _scrollSign * _scaleMultiplier;
                _currentScale = Mathf.Clamp(_currentScale, _scaleMin, _scaleMax);

                SetScale(_currentScale);
            }
        }
    }

    private void SetScale(float scaleFactor)
    {
        var scale = _positionContainer.localScale;

        _positionContainer.localScale = new Vector3(scaleFactor, scaleFactor, scale.z);
    }
}
