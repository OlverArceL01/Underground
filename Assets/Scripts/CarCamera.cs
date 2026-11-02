
using UnityEngine;
using UnityEngine.InputSystem;

public class CarCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Settings")]
    public float distance = 7f;
    public float height = 3f;
    public float lookHeight = 1f;

    [Header("Follow")]
    public float followSpeed = 8f;
    public float rotationSpeed = 5f;
    public float rotationSmoothness = 5f;

    [Header("Manual Rotation")]
    public float mouseSensitivity = 0.15f;
    public float minPitch = -10f;
    public float maxPitch = 65f;

    [Header("Zoom")]
    public float zoomSpeed = 2f;
    public float minDistance = 2f;
    public float maxDistance = 15f;

    private float yaw;
    private float pitch = 12f;
    private float distanceVelocity;

    void Start()
    {
        if (target != null)
        {
            yaw = target.eulerAngles.y;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        bool manualRotation = false;

        // Manual camera rotation
        if (Mouse.current != null &&
            Mouse.current.rightButton.isPressed)
        {
            manualRotation = true;

            yaw += Mouse.current.delta.x.ReadValue()
                * mouseSensitivity;

            pitch -= Mouse.current.delta.y.ReadValue()
                * mouseSensitivity;

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        // Automatically follow vehicle direction
        if (!manualRotation)
        {
            float targetYaw = target.eulerAngles.y;

            yaw = Mathf.LerpAngle(
                yaw,
                targetYaw,
                rotationSmoothness * Time.deltaTime
            );
        }

        // Zoom
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;

            distance -= scroll * zoomSpeed * 0.01f;

            distance = Mathf.Clamp(
                distance, minDistance, maxDistance
            );
        }

        // Camera position
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 offset = rotation * new Vector3(
            0f, height, -distance
        );

        Vector3 desiredPosition = target.position + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );

        transform.LookAt(
            target.position + Vector3.up * lookHeight
        );
    }
}
