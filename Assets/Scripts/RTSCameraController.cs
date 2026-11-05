using UnityEngine;
using UnityEngine.InputSystem;

public class RTSCameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 20f;
    public float fastMoveMultiplier = 2f;
    public float movementSmoothing = 8f;

    [Header("Zoom")]
    public Transform cameraPivot;
    public float zoomSpeed = 10f;
    public float minZoom = 5f;
    public float maxZoom = 60f;

    [Header("Rotation")]
    public bool useMiddleMouseRotation = true;
    public float rotationSpeed = 15f;
    public float minPitch = 0f;
    public float maxPitch = 90f;

    [Header("Edge Scrolling")]
    public bool edgeScrolling = true;
    public float edgeSize = 15f;

    private Vector3 targetPosition;
    private float currentZoom = 60f;
    private float currentYaw;
    private float currentPitch = 20f;

    void Start()
    {
        targetPosition = transform.position;

        currentYaw = transform.eulerAngles.y;
        currentPitch = 20f;

        if (cameraPivot != null)
        {
            currentZoom = Mathf.Abs(
                cameraPivot.GetChild(0).localPosition.z
            );
        }
    }

    void Update()
    {
        HandleMovement();
        HandleZoom();
        HandleRotation();
    }

    void LateUpdate()
    {
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            movementSmoothing * Time.deltaTime
        );

        if (cameraPivot != null)
        {
            cameraPivot.rotation = Quaternion.Euler(
                currentPitch,
                currentYaw,
                0f
            );

            Vector3 zoomPosition = new Vector3(
                0f, 0f, -currentZoom
            );

            cameraPivot.GetChild(0).localPosition = zoomPosition;
        }
    }

    void HandleMovement()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard == null) return;

        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard.aKey.isPressed) horizontal -= 1f;
        if (keyboard.dKey.isPressed) horizontal += 1f;
        if (keyboard.sKey.isPressed) vertical -= 1f;
        if (keyboard.wKey.isPressed) vertical += 1f;

        Quaternion yawRotation = Quaternion.Euler(0f, currentYaw, 0f);
        Vector3 forward = yawRotation * Vector3.forward;
        Vector3 right = yawRotation * Vector3.right;

        Vector3 direction = forward * vertical + right * horizontal;

        // Edge scrolling
        if (edgeScrolling && mouse != null)
        {
            Vector2 mousePosition = mouse.position.ReadValue();

            if (mousePosition.x <= edgeSize)
                direction -= right;

            if (mousePosition.x >= Screen.width - edgeSize)
                direction += right;

            if (mousePosition.y <= edgeSize)
                direction -= forward;

            if (mousePosition.y >= Screen.height - edgeSize)
                direction += forward;
        }

        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        float speed = moveSpeed;

        if (keyboard.leftShiftKey.isPressed)
            speed *= fastMoveMultiplier;

        targetPosition += direction * speed * Time.deltaTime;
    }

    void HandleZoom()
    {
        if (Mouse.current == null) return;

        float scroll = Mouse.current.scroll.ReadValue().y;

        currentZoom -= scroll * zoomSpeed * 0.01f;

        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
    }

    void HandleRotation()
    {
        if (Mouse.current == null) return;

        // Middle mouse button rotation
        bool draggingMiddleMouse = useMiddleMouseRotation && Mouse.current.middleButton.isPressed;
        bool draggingAltLeftMouse = Keyboard.current != null && Keyboard.current.leftAltKey.isPressed && Mouse.current.leftButton.isPressed;
        if (draggingMiddleMouse || draggingAltLeftMouse)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            currentYaw += mouseDelta.x * rotationSpeed * Time.deltaTime;

            currentPitch -= mouseDelta.y * rotationSpeed * Time.deltaTime;

            currentPitch = Mathf.Clamp(
                currentPitch,
                minPitch,
                maxPitch
            );
        }

        // Keyboard rotation
        if (Keyboard.current != null)
        {
            if (Keyboard.current.qKey.isPressed)
                currentYaw -= rotationSpeed * Time.deltaTime;

            if (Keyboard.current.eKey.isPressed)
                currentYaw += rotationSpeed * Time.deltaTime;
        }
    }
}