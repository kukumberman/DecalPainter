using UnityEngine;

public sealed class SimpleCameraController : MonoBehaviour
{
    [SerializeField]
    private Transform _origin;

    [SerializeField]
    private Transform _camera;

    [SerializeField]
    private KeyCode _key = KeyCode.Mouse1;

    [Range(0f, 1f)]
    [SerializeField]
    private float _smoothFactor01 = 0.5f;

    [SerializeField]
    private float _sensitivityMultiplier = 1;

    [Header("Zoom")]
    [SerializeField]
    private float _scrollMultiplier = 10f;

    [SerializeField]
    private float _distanceMin = 1f;

    [SerializeField]
    private float _distanceMax = 10f;

    private float _t01;

    private Vector3 _eulerAngles;
    private Vector3 _currentEulerAngles;

    private float _targetDistance;
    private float _currentDistance;

    private void Start()
    {
        _eulerAngles = _origin.localEulerAngles;

        _targetDistance = -1f * _camera.localPosition.z;
        _currentDistance = _targetDistance;
    }

    private void Update()
    {
        if (Input.GetKey(_key))
        {
            ChangeAngles();
            ChangeZoom();
        }

        _t01 = _smoothFactor01 * 50f * Time.deltaTime;

        LerpAngles();
        LerpDistance();
    }

    private void ChangeAngles()
    {
        var multiplier = _sensitivityMultiplier * 50f * Time.deltaTime;

        _eulerAngles.x += Input.GetAxisRaw("Mouse Y") * multiplier * -1f;
        _eulerAngles.x = Mathf.Clamp(_eulerAngles.x, -90f, 90f);

        _eulerAngles.y += Input.GetAxisRaw("Mouse X") * multiplier;
    }

    private void ChangeZoom()
    {
        var scrollY = Input.mouseScrollDelta.y;

        if (scrollY != 0)
        {
            var sign = Mathf.Sign(scrollY);

            _targetDistance += -1f * sign * _scrollMultiplier;
            _targetDistance = Mathf.Clamp(_targetDistance, _distanceMin, _distanceMax);
        }
    }

    private void LerpAngles()
    {
        _currentEulerAngles.x = Mathf.LerpAngle(_currentEulerAngles.x, _eulerAngles.x, _t01);
        _currentEulerAngles.y = Mathf.LerpAngle(_currentEulerAngles.y, _eulerAngles.y, _t01);

        _origin.localEulerAngles = _currentEulerAngles;
    }

    private void LerpDistance()
    {
        _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, _t01);

        var position = _camera.localPosition;
        position.z = -1f * _currentDistance;
        _camera.localPosition = position;
    }
}
