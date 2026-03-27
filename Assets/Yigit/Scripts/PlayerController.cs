using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float gravity = 20f;

    [Header("Look Settings")]
    public Camera playerCamera;
    public float lookSpeed = 2f;
    public float lookXLimit = 75f;

    [Header("Collect Settings")]
    public float collectDistance = 3f;
    public KeyCode collectKey = KeyCode.E;
    public int requiredCollectCountForThrow = 10;
    public LayerMask collectibleLayers = ~0;

    [Header("Throw Settings")]
    public KeyCode throwKey = KeyCode.F;
    public GameObject throwableObjectPrefab;
    public Transform throwOrigin;
    public float throwForce = 18f;

    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0f;
    private int collectedCount = 0;

    [HideInInspector]
    public bool canMove = true;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (canMove)
        {
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            bool isRunning = Input.GetKey(KeyCode.LeftShift);
            float currentSpeed = isRunning ? runSpeed : walkSpeed;

            float moveZ = Input.GetAxis("Vertical") * currentSpeed;
            float moveX = Input.GetAxis("Horizontal") * currentSpeed;

            moveDirection.x = (forward.x * moveZ) + (right.x * moveX);
            moveDirection.z = (forward.z * moveZ) + (right.z * moveX);

            float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

            transform.Rotate(0f, mouseX, 0f);

            if (playerCamera != null)
            {
                rotationX -= mouseY;
                rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

                if (playerCamera.transform == transform)
                {
                    // If camera is on the same object, preserve yaw from body rotation.
                    playerCamera.transform.rotation = Quaternion.Euler(rotationX, transform.eulerAngles.y, 0f);
                }
                else
                {
                    // If camera is a child, apply only local pitch.
                    playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
                }
            }
        }

        if (Input.GetKeyDown(collectKey))
        {
            TryCollectNearby();
        }

        if (Input.GetKeyDown(throwKey))
        {
            TryThrowObject();
        }

        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }
        else
        {
            moveDirection.y = -0.1f;
        }

        characterController.Move(moveDirection * Time.deltaTime);
    }

    private void TryCollectNearby()
    {
        Vector3 rayOrigin = playerCamera != null ? playerCamera.transform.position : transform.position + Vector3.up;
        Vector3 rayDirection = playerCamera != null ? playerCamera.transform.forward : transform.forward;

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, collectDistance, collectibleLayers, QueryTriggerInteraction.Collide))
        {
            CollectibleObject collectible = hit.collider.GetComponentInParent<CollectibleObject>();

            if (collectible != null && !collectible.IsCollected)
            {
                collectible.Collect();
                collectedCount++;
            }
        }
    }

    private void TryThrowObject()
    {
        if (collectedCount < requiredCollectCountForThrow)
        {
            return;
        }

        if (throwableObjectPrefab == null)
        {
            return;
        }

        Transform origin = throwOrigin != null ? throwOrigin : (playerCamera != null ? playerCamera.transform : transform);
        GameObject thrownObject = Instantiate(throwableObjectPrefab, origin.position, Quaternion.identity);

        Rigidbody rb = thrownObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = thrownObject.AddComponent<Rigidbody>();
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(origin.forward * throwForce, ForceMode.Impulse);
    }
}