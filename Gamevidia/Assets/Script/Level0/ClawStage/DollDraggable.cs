using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DollDraggable : MonoBehaviour
{
    private Camera mainCamera;
    private bool isDragging = false;
    private Vector3 offset;
    private Rigidbody2D rb;

    [Header("Settings")]
    [SerializeField] private float minY = -3f; // Batas bawah drag
    [SerializeField] private float maxY = 3f;  // Batas atas drag

    void Awake()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody2D>();
    }

    void OnMouseDown()
    {
        if (!enabled) return;

        isDragging = true;

        // Disable physics saat drag
        if (rb != null)
            rb.bodyType = RigidbodyType2D.Kinematic;

        Vector3 mousePos = GetMouseWorldPosition();
        offset = transform.position - mousePos;
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 mousePos = GetMouseWorldPosition();
        Vector3 targetPos = mousePos + offset;

        // Clamp Y position
        targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);
        targetPos.z = 0f; // Force Z = 0 untuk 2D

        transform.position = targetPos;
    }

    void OnMouseUp()
    {
        if (!isDragging) return;

        isDragging = false;

        // Enable physics lagi (untuk gravity kalau miss basket)
        if (rb != null)
            rb.bodyType = RigidbodyType2D.Dynamic;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(mainCamera.transform.position.z);
        return mainCamera.ScreenToWorldPoint(mousePos);
    }

    // Public method untuk disable drag (saat di-grab crane)
    public void SetDraggable(bool draggable)
    {
        enabled = draggable;

        if (!draggable && rb != null)
            rb.bodyType = RigidbodyType2D.Dynamic;
    }
}