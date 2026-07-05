// Codex World — God-view camera
// Mouse-driven orbit view centered on the level:
//   Left-click drag  — rotate: horizontal = orbit, vertical = tilt angle
//   Right-click drag — move the view up/down (pans the focus point)
//   Mouse scroll     — zoom
// Drags that start over a UI element are ignored, so clicking command
// buttons never moves the camera. Uses the new Input System.

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class GodViewCamera : MonoBehaviour
{
    // Configured by GameController from its Inspector properties.
    public float pitch = 55f;
    public float yaw = 45f;
    public float zoomFactor = 2.2f;      // camera distance as a multiple of grid radius

    [Header("Mouse Controls")]
    [Tooltip("Degrees of rotation per pixel of left-click drag.")]
    public float dragRotateSpeed = 0.25f;
    [Tooltip("World units the view moves per pixel of right-click drag.")]
    public float panSpeed = 0.01f;
    [Tooltip("Camera tilt limits when dragging vertically (degrees).")]
    public float minPitch = 20f;
    public float maxPitch = 85f;

    Vector3 _center;
    float _distance = 8f;
    float _minDistance = 4f;
    float _maxDistance = 20f;
    float _baseCenterY;
    bool _rotating;
    bool _panning;

    public void SetTarget(Vector3 center, float gridRadius)
    {
        _center = center;
        _baseCenterY = center.y;
        _distance = Mathf.Clamp(gridRadius * zoomFactor, 6f, 18f);
        _minDistance = Mathf.Max(3f, gridRadius);
        _maxDistance = gridRadius * 4f;
        Apply();
    }

    static bool PointerOverUI() =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

    void LateUpdate()
    {
        var mouse = Mouse.current;
        if (mouse != null)
        {
            // Start drags only when the press begins outside the UI.
            if (mouse.leftButton.wasPressedThisFrame) _rotating = !PointerOverUI();
            if (mouse.leftButton.wasReleasedThisFrame) _rotating = false;
            if (mouse.rightButton.wasPressedThisFrame) _panning = !PointerOverUI();
            if (mouse.rightButton.wasReleasedThisFrame) _panning = false;

            var delta = mouse.delta.ReadValue();

            if (_rotating)
            {
                yaw += delta.x * dragRotateSpeed;
                pitch = Mathf.Clamp(pitch - delta.y * dragRotateSpeed, minPitch, maxPitch);
            }

            if (_panning)
            {
                // Drag up = view moves up; height stays near the grid so you can't get lost.
                float y = _center.y - delta.y * panSpeed;
                _center.y = Mathf.Clamp(y, _baseCenterY - 3f, _baseCenterY + 6f);
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                _distance = Mathf.Clamp(_distance - scroll * 0.01f * _distance, _minDistance, _maxDistance);
        }

        Apply();
    }

    void Apply()
    {
        var rot = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = rot;
        transform.position = _center - rot * Vector3.forward * _distance;
    }
}
