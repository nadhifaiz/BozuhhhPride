using System;
using System.Collections;
using UnityEngine;

public class CraneController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float minX = -5f;
    [SerializeField] private float maxX = 5f;
    [SerializeField] private float topY = 4f;     // Posisi atas
    [SerializeField] private float bottomY = -4f;  // Posisi bawah (grab)
    [SerializeField] private float descendSpeed = 3f;
    [SerializeField] private float ascendSpeed = 2f;
    [SerializeField] private float grabRadius = 0.5f;
    [SerializeField] private float detachDelay = 0.5f; // Delay sebelum boneka jatuh

    [Header("References")]
    [SerializeField] private Transform grabPoint; // Point di bawah crane untuk detect doll

    public event Action OnDollDropped;
    public event Action OnFirstGrabAttempt;

    private enum CraneState
    {
        Idle,
        Descending,
        Checking,
        Ascending
    }

    private CraneState state = CraneState.Idle;
    private GameObject grabbedDoll;
    private bool inputEnabled = false;
    private bool hasTriggeredFirstGrab = false;

    void Update()
    {
        if (!inputEnabled) return;

        // Horizontal movement (hanya saat Idle)
        if (state == CraneState.Idle)
        {
            HandleMovement();
            HandleGrabInput();
        }
    }

    private void HandleMovement()
    {
        float horizontal = 0f;

        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            horizontal = -1f;
        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            horizontal = 1f;

        if (horizontal != 0f)
        {
            Vector3 pos = transform.position;
            pos.x += horizontal * moveSpeed * Time.deltaTime;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            transform.position = pos;
        }
    }

    private void HandleGrabInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Trigger event pertama kali player pencet space
            if (!hasTriggeredFirstGrab)
            {
                hasTriggeredFirstGrab = true;
                Debug.Log("🔥 FIRST GRAB ATTEMPT EVENT INVOKED"); // ← TAMBAHKAN INI
                OnFirstGrabAttempt?.Invoke();
            }

            StartCoroutine(GrabSequence());
        }
    }

    private IEnumerator GrabSequence()
    {
        state = CraneState.Descending;

        // DESCEND
        while (transform.position.y > bottomY)
        {
            Vector3 pos = transform.position;
            pos.y -= descendSpeed * Time.deltaTime;
            pos.y = Mathf.Max(pos.y, bottomY);
            transform.position = pos;
            yield return null;
        }

        // CHECK FOR DOLL
        state = CraneState.Checking;
        yield return new WaitForSeconds(0.2f); // Small delay

        Vector2 checkPos = grabPoint != null ? grabPoint.position : transform.position;
        Collider2D hit = Physics2D.OverlapCircle(checkPos, grabRadius);

        if (hit != null && hit.CompareTag("Doll"))
        {
            // GRAB DOLL
            grabbedDoll = hit.gameObject;
            grabbedDoll.transform.parent = transform;

            // Disable physics & draggable
            Rigidbody2D rb = grabbedDoll.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.bodyType = RigidbodyType2D.Kinematic;

            DollDraggable draggable = grabbedDoll.GetComponent<DollDraggable>();
            if (draggable != null)
                draggable.SetDraggable(false);

            Debug.Log("Doll grabbed!");
        }

        // ASCEND
        state = CraneState.Ascending;
        StartCoroutine(AscendWithDoll());
    }

    private IEnumerator AscendWithDoll()
    {
        // Naik ke atas
        while (transform.position.y < topY)
        {
            Vector3 pos = transform.position;
            pos.y += ascendSpeed * Time.deltaTime;
            pos.y = Mathf.Min(pos.y, topY);
            transform.position = pos;
            yield return null;
        }

        // FORCE DETACH (boneka pasti jatuh)
        if (grabbedDoll != null)
        {
            yield return new WaitForSeconds(detachDelay);

            grabbedDoll.transform.parent = null;

            Rigidbody2D rb = grabbedDoll.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.AddForce(Vector2.down * 3f, ForceMode2D.Impulse);
            }

            Debug.Log("[CRANE] 💧 Doll dropped! Invoking OnDollDropped event"); // ← TAMBAH INI
            OnDollDropped?.Invoke();

            grabbedDoll = null;
        }

        state = CraneState.Idle;
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
    }

    void OnDrawGizmosSelected()
    {
        // Visual debug grab radius
        Vector3 checkPos = grabPoint != null ? grabPoint.position : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(checkPos, grabRadius);
    }
}