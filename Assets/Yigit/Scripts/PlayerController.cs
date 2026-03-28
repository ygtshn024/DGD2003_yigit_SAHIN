using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("UI")]
    [Tooltip("Sürükleyebileceğin: Text (TMP) objesi. Boşsa Trash Count UI Parent kullanılır.")]
    public TextMeshProUGUI trashCountText;
    [Tooltip("Panel veya Canvas kökü; içinde TextMeshProUGUI aranır.")]
    public GameObject trashCountUIParent;
    [Tooltip("İsteğe bağlı: Hierarchy'deki Text (TMP) objesinin tam adı (referans boşsa bir kez aranır).")]
    public string trashCountTextObjectName = "";
    public string trashCountFormat = "Toplanan Çöp: {0}";

    [Header("Throw Settings")]
    public KeyCode throwKey = KeyCode.F;
    public GameObject throwableObjectPrefab;
    public Transform throwOrigin;
    public float throwForce = 18f;
    public float throwSpawnForwardOffset = 0.35f;

    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0f;
    private int collectedCount = 0;
    private CollectibleObject currentHighlightedCollectible;
    private bool loggedMissingTrashUi;

    [HideInInspector]
    public bool canMove = true;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        ResolveTrashCountTextReference();
        UpdateTrashCountUI();
    }

    private void ResolveTrashCountTextReference()
    {
        if (trashCountText != null)
        {
            return;
        }

        if (trashCountUIParent != null)
        {
            trashCountText = trashCountUIParent.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (trashCountText == null && !string.IsNullOrEmpty(trashCountTextObjectName))
        {
            GameObject named = GameObject.Find(trashCountTextObjectName);
            if (named != null)
            {
                trashCountText = named.GetComponent<TextMeshProUGUI>();
            }
        }

        if (trashCountText == null)
        {
            Debug.LogWarning(
                "FirstPersonController: Çöp sayısı için TextMeshProUGUI bulunamadı. " +
                "Inspector'da Trash Count Text alanına Canvas içindeki Text (TMP) objesini sürükle veya Trash Count UI Parent'a paneli ver.");
        }
    }

    private void Update()
    {
        UpdateCollectibleHighlight();

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

        int layerMask = collectibleLayers.value == 0 ? Physics.DefaultRaycastLayers : collectibleLayers;

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, collectDistance, layerMask, QueryTriggerInteraction.Collide))
        {
            CollectibleObject collectible = hit.collider.GetComponentInParent<CollectibleObject>();

            if (collectible != null && !collectible.IsCollected)
            {
                collectible.Collect();
                collectedCount++;
                Debug.Log("Collected object count: " + collectedCount);
                UpdateTrashCountUI();
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

        collectedCount -= requiredCollectCountForThrow;
        Debug.Log("Used " + requiredCollectCountForThrow + " collectibles for throw. Remaining: " + collectedCount);
        UpdateTrashCountUI();

        Transform origin = throwOrigin != null ? throwOrigin : (playerCamera != null ? playerCamera.transform : transform);
        Vector3 spawnPosition = origin.position + (origin.forward * throwSpawnForwardOffset);
        GameObject thrownObject = Instantiate(throwableObjectPrefab, spawnPosition, origin.rotation);

        Rigidbody rb = thrownObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = thrownObject.AddComponent<Rigidbody>();
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(origin.forward * throwForce, ForceMode.Impulse);
    }

    private void UpdateCollectibleHighlight()
    {
        Vector3 rayOrigin = playerCamera != null ? playerCamera.transform.position : transform.position + Vector3.up;
        Vector3 rayDirection = playerCamera != null ? playerCamera.transform.forward : transform.forward;

        CollectibleObject lookedCollectible = null;

        int layerMask = collectibleLayers.value == 0 ? Physics.DefaultRaycastLayers : collectibleLayers;

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, collectDistance, layerMask, QueryTriggerInteraction.Collide))
        {
            lookedCollectible = hit.collider.GetComponentInParent<CollectibleObject>();

            if (lookedCollectible != null && lookedCollectible.IsCollected)
            {
                lookedCollectible = null;
            }
        }

        if (currentHighlightedCollectible == lookedCollectible)
        {
            return;
        }

        if (currentHighlightedCollectible != null)
        {
            currentHighlightedCollectible.SetHighlighted(false);
        }

        currentHighlightedCollectible = lookedCollectible;

        if (currentHighlightedCollectible != null)
        {
            currentHighlightedCollectible.SetHighlighted(true);
        }
    }

    private void UpdateTrashCountUI()
    {
        bool updated = TrashCountDisplay.SetCount(collectedCount, trashCountFormat, requiredCollectCountForThrow);

        if (trashCountText == null)
        {
            ResolveTrashCountTextReference();
        }

        if (trashCountText != null)
        {
            string value = string.Format(trashCountFormat, collectedCount);
            trashCountText.text = value;
            trashCountText.color = collectedCount >= requiredCollectCountForThrow ? Color.red : Color.white;
            trashCountText.enabled = true;
            trashCountText.gameObject.SetActive(true);

            Canvas canvas = trashCountText.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = true;
                if (!canvas.gameObject.activeInHierarchy)
                {
                    canvas.gameObject.SetActive(true);
                }
            }

            CanvasGroup canvasGroup = trashCountText.GetComponentInParent<CanvasGroup>();
            if (canvasGroup != null && canvasGroup.alpha < 0.01f)
            {
                canvasGroup.alpha = 1f;
            }

            trashCountText.ForceMeshUpdate(true);
            Canvas.ForceUpdateCanvases();
            updated = true;
        }

        if (!updated && !loggedMissingTrashUi)
        {
            loggedMissingTrashUi = true;
            Debug.LogWarning(
                "Çöp sayısı ekranda güncellenmiyor. Şunlardan birini yap: " +
                "(1) Canvas'taki Text (TMP) objesine Add Component > TrashCountDisplay ekle — Player'a referans gerekmez. " +
                "(2) Veya Player > FirstPersonController > Trash Count Text alanına bu Text (TMP) objesini sürükle (UGUI, 3D Text değil).");
        }
    }
}