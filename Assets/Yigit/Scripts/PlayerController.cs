using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

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
    [Tooltip("E ile isin; cop torbasi da bu layer mask icinde olmali (genelde Everything).")]
    public LayerMask collectibleLayers = ~0;

    [Header("UI")]
    [Tooltip("Sürükleyebileceğin: Text (TMP) objesi. Boşsa Trash Count UI Parent kullanılır.")]
    public TextMeshProUGUI trashCountText;
    [Tooltip("Panel veya Canvas kökü; içinde TextMeshProUGUI aranır.")]
    public GameObject trashCountUIParent;
    [Tooltip("İsteğe bağlı: Hierarchy'deki Text (TMP) objesinin tam adı (referans boşsa bir kez aranır).")]
    public string trashCountTextObjectName = "";
    [FormerlySerializedAs("trashCountFormat")]
    [Tooltip("İlerleme metni: {0} = sahnede kalan toplanabilir sayı (topladıkça azalır).")]
    public string collectProgressFormat = "Kalan: {0}";
    [Tooltip("Sahnedeki tüm toplanabilir objeler bitince gösterilir.")]
    public string allCollectiblesCompletedMessage = "Tebrikler! Tum toplanabilir objeleri topladin!";
    [FormerlySerializedAs("inventoryFullHintFormat")]
    [Tooltip("Envanter doluyken (fırlatma eşiği) alt satırda gösterilir.")]
    public string inventoryFullHint = "Copleri Bosalt \"F\"";
    [Tooltip("Cop kutusu hedefi (toplam toplanabilir / 10) tamamlaninca.")]
    public string gameWonByBinMessage = "Tebrikler! Cop kutusu hedefi tamam!";
    [Tooltip("Oyun devam ederken: {0} = cop kutusuna giden torba, {1} = gereken (toplanabilir sayisi / 10).")]
    public string binProgressFormat = "Cop kutusu: {0} / {1}";

    [Header("Timer / round")]
    [Tooltip("Süre kazaninca dursun diye bagla (bos birakilabilir).")]
    public GameCountdownTimer missionCountdownTimer;

    [Header("Round end")]
    [Tooltip("Kazaninca tebrik metnini gosterdikten kac saniye sonra ana menuye donulsun.")]
    [SerializeField]
    private float returnToMainMenuAfterWinSeconds = 4f;

    [Header("Throw Settings")]
    public KeyCode throwKey = KeyCode.F;
    public GameObject throwableObjectPrefab;
    public Transform throwOrigin;
    public float throwForce = 18f;
    public float throwSpawnForwardOffset = 0.35f;
    [Tooltip("Should be a child of the camera that pitches (Main Camera). Otherwise the bag won't follow look up/down.")]
    public Transform carryHoldPoint;
    [Tooltip("Local position on carry parent. With Carry Hold Point, use (0,0,0) if the empty is already placed; without it, this offsets from the camera.")]
    public Vector3 carryLocalOffset = new Vector3(0.32f, -0.22f, 0.58f);
    [Tooltip("Local rotation while carried (tweak if mesh faces wrong way).")]
    public Vector3 carryLocalEulerAngles = Vector3.zero;

    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0f;
    private int collectedCount = 0;
    private int totalSceneCollectibles;
    private int sceneCollectiblesGathered;
    private CollectibleObject currentHighlightedCollectible;
    private bool loggedMissingTrashUi;
    private GameObject carriedThrowable;
    private int requiredSuccessfulBinThrows;
    private bool winSequenceApplied;
    private bool timeExpiredSequenceActive;

    /// <summary>True after collect-all or cop kutusu hedefi (timer bunu bilir).</summary>
    public static bool RoundWon { get; private set; }

    private bool GameplayBlocked => winSequenceApplied || timeExpiredSequenceActive;

    [HideInInspector]
    public bool canMove = true;

    private void Start()
    {
        RoundWon = false;
        timeExpiredSequenceActive = false;
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        ResolveTrashCountTextReference();
        CountSceneCollectibles();
        requiredSuccessfulBinThrows = totalSceneCollectibles / 10;
        TrashBinProgress.ResetCounters();
        TrashBinProgress.OnHitTrashBin += OnTrashHitBin;
        UpdateTrashCountUI();
        WarnIfCarryHoldPointNotUnderCamera();
    }

    private void OnDestroy()
    {
        TrashBinProgress.OnHitTrashBin -= OnTrashHitBin;
    }

    private void OnTrashHitBin()
    {
        UpdateTrashCountUI();
    }

    private void ApplyWinIfNeeded(bool won)
    {
        if (!won || winSequenceApplied)
        {
            return;
        }

        winSequenceApplied = true;
        RoundWon = true;
        canMove = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (missionCountdownTimer != null)
        {
            missionCountdownTimer.FreezeCountdown();
        }

        StartCoroutine(ReturnToMainMenuAfterWinRoutine());
    }

    private IEnumerator ReturnToMainMenuAfterWinRoutine()
    {
        yield return new WaitForSecondsRealtime(returnToMainMenuAfterWinSeconds);
        GameFlow.LoadMainMenu();
    }

    public void BeginTimeExpiredSequence()
    {
        if (winSequenceApplied || timeExpiredSequenceActive)
        {
            return;
        }

        timeExpiredSequenceActive = true;
        canMove = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void WarnIfCarryHoldPointNotUnderCamera()
    {
        if (carryHoldPoint == null || playerCamera == null)
        {
            return;
        }

        Transform camT = playerCamera.transform;
        if (carryHoldPoint != camT && !carryHoldPoint.IsChildOf(camT))
        {
            Debug.LogWarning(
                "FirstPersonController: Carry Hold Point, Player Camera'in altinda degil. " +
                "Cantayi gercekten nereye baktigina gore tasimak icin Hold Point'i Main Camera'nin child'i yap.");
        }
    }

    private void LateUpdate()
    {
        if (GameplayBlocked)
        {
            return;
        }

        if (carriedThrowable == null)
        {
            return;
        }

        Transform attach = GetCarryAttachTransform();
        if (carriedThrowable.transform.parent != attach)
        {
            return;
        }

        ApplyCarriedThrowableLocalPose(carriedThrowable.transform);
    }

    private void ApplyCarriedThrowableLocalPose(Transform bagTransform)
    {
        bagTransform.localPosition = carryLocalOffset;
        bagTransform.localRotation = Quaternion.Euler(carryLocalEulerAngles);
    }

    private void CountSceneCollectibles()
    {
        CollectibleObject[] inScene = FindObjectsByType<CollectibleObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        totalSceneCollectibles = inScene.Length;
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
        if (!GameplayBlocked)
        {
            UpdateCollectibleHighlight();
        }

        if (!GameplayBlocked && canMove)
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

        if (!GameplayBlocked && Input.GetKeyDown(collectKey))
        {
            TryCollectNearby();
        }

        if (!GameplayBlocked && Input.GetKeyDown(throwKey))
        {
            TryCarryOrThrowThrowable();
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

        if (!Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, collectDistance, layerMask, QueryTriggerInteraction.Collide))
        {
            return;
        }

        if (carriedThrowable == null && TryGetWorldThrowableBag(hit.collider, out GameObject bagRoot))
        {
            AttachThrowableAsCarried(bagRoot);
            return;
        }

        if (collectedCount >= requiredCollectCountForThrow)
        {
            return;
        }

        CollectibleObject collectible = hit.collider.GetComponentInParent<CollectibleObject>();

        if (collectible != null && !collectible.IsCollected)
        {
            collectible.Collect();
            collectedCount++;
            sceneCollectiblesGathered++;
            Debug.Log("Collected object count: " + collectedCount);
            UpdateTrashCountUI();
        }
    }

    /// <summary>Dünya üzerindeki fırlatılmış çöp torbası (ThrownObjectDestroyOnImpact).</summary>
    private static bool TryGetWorldThrowableBag(Collider hitCollider, out GameObject bagRoot)
    {
        bagRoot = null;
        if (hitCollider == null)
        {
            return false;
        }

        ThrownObjectDestroyOnImpact marker = hitCollider.GetComponentInParent<ThrownObjectDestroyOnImpact>();
        if (marker == null)
        {
            return false;
        }

        bagRoot = marker.gameObject;
        return true;
    }

    private void AttachThrowableAsCarried(GameObject bag)
    {
        if (bag == null || carriedThrowable != null)
        {
            return;
        }

        Transform attach = GetCarryAttachTransform();
        Rigidbody rb = bag.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = bag.AddComponent<Rigidbody>();
        }

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        bag.transform.SetParent(attach, false);
        ApplyCarriedThrowableLocalPose(bag.transform);

        Collider[] colliders = bag.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }

        carriedThrowable = bag;
    }

    private void TryCarryOrThrowThrowable()
    {
        if (carriedThrowable != null)
        {
            ReleaseAndThrowCarried();
            return;
        }

        if (collectedCount < requiredCollectCountForThrow)
        {
            return;
        }

        if (throwableObjectPrefab == null)
        {
            return;
        }

        collectedCount -= requiredCollectCountForThrow;
        Debug.Log("Threw bag. Remaining trash count: " + collectedCount);
        UpdateTrashCountUI();

        InstantiateAndImpulseThrow();
    }

    private void InstantiateAndImpulseThrow()
    {
        if (throwableObjectPrefab == null)
        {
            return;
        }

        Transform origin = throwOrigin != null ? throwOrigin : (playerCamera != null ? playerCamera.transform : transform);
        Vector3 spawnPosition = origin.position + (origin.forward * throwSpawnForwardOffset);
        GameObject thrownObject = Instantiate(throwableObjectPrefab, spawnPosition, origin.rotation);

        Rigidbody rb = thrownObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = thrownObject.AddComponent<Rigidbody>();
        }

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(origin.forward * throwForce, ForceMode.Impulse);
    }

    private Transform GetCarryAttachTransform()
    {
        if (carryHoldPoint != null)
        {
            return carryHoldPoint;
        }

        if (playerCamera != null)
        {
            return playerCamera.transform;
        }

        return transform;
    }

    private void ReleaseAndThrowCarried()
    {
        if (carriedThrowable == null)
        {
            return;
        }

        Transform origin = throwOrigin != null ? throwOrigin : (playerCamera != null ? playerCamera.transform : transform);

        carriedThrowable.transform.SetParent(null, true);

        Rigidbody rb = carriedThrowable.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = carriedThrowable.AddComponent<Rigidbody>();
        }

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Collider[] colliders = carriedThrowable.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = true;
            }
        }

        rb.AddForce(origin.forward * throwForce, ForceMode.Impulse);
        carriedThrowable = null;
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

            if (lookedCollectible != null && collectedCount >= requiredCollectCountForThrow)
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
        bool allSceneDone = totalSceneCollectibles > 0 && sceneCollectiblesGathered >= totalSceneCollectibles;
        bool binGoalMet = requiredSuccessfulBinThrows > 0 && TrashBinProgress.HitsToBin >= requiredSuccessfulBinThrows;
        ApplyWinIfNeeded(binGoalMet || allSceneDone);

        bool inventoryFull = collectedCount >= requiredCollectCountForThrow;
        string inventoryHint = (!allSceneDone && inventoryFull && !string.IsNullOrEmpty(inventoryFullHint))
            ? inventoryFullHint
            : null;

        string binProgressLine = null;
        if (requiredSuccessfulBinThrows > 0 && !binGoalMet && totalSceneCollectibles > 0 && !allSceneDone)
        {
            binProgressLine = string.Format(binProgressFormat, TrashBinProgress.HitsToBin, requiredSuccessfulBinThrows);
        }

        bool updated = TrashCountDisplay.SetProgress(
            sceneCollectiblesGathered,
            totalSceneCollectibles,
            allSceneDone,
            collectProgressFormat,
            allCollectiblesCompletedMessage,
            collectedCount,
            requiredCollectCountForThrow,
            inventoryHint,
            binGoalMet,
            gameWonByBinMessage,
            binProgressLine);

        if (trashCountText == null)
        {
            ResolveTrashCountTextReference();
        }

        if (trashCountText != null)
        {
            string value;
            Color textColor;
            if (binGoalMet)
            {
                value = gameWonByBinMessage;
                textColor = new Color(0.35f, 0.95f, 0.45f);
            }
            else if (allSceneDone)
            {
                value = allCollectiblesCompletedMessage;
                textColor = new Color(0.35f, 0.95f, 0.45f);
            }
            else
            {
                int remainingInScene = Mathf.Max(0, totalSceneCollectibles - sceneCollectiblesGathered);
                value = string.Format(collectProgressFormat, remainingInScene);
                if (inventoryHint != null)
                {
                    value += "\n" + inventoryHint;
                }

                if (binProgressLine != null)
                {
                    value += "\n" + binProgressLine;
                }

                textColor = inventoryFull ? Color.red : Color.white;
            }

            trashCountText.text = value;
            trashCountText.color = textColor;
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