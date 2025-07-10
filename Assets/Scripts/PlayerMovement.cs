using UnityEngine;
using UnityEngine.UI; // ��������� ��� UI
using TMPro; // ��������� ��� TextMeshPro (�����������)

[RequireComponent(typeof(CharacterController), typeof(AudioSource))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 12f;          // �������� ������
    [SerializeField] private float runSpeed = 18f;           // �������� ����
    [SerializeField] private float crouchSpeed = 6f;         // �������� ����������
    [SerializeField] private float gravity = -9.81f;         // ���� ����������
    [SerializeField] private float jumpHeight = 3f;          // ������ ������

    [Header("Camera Settings")]
    [SerializeField] private Camera playerCamera;            // ������ �� ������
    [SerializeField] private Transform cameraTarget; // ��������� ��� ������
    [SerializeField] private float normalFOV = 60f;          // ���������� ���� ������
    [SerializeField] private float runFOV = 70f;             // ���� ������ ��� ����
    [SerializeField] private float fovChangeSpeed = 5f;      // �������� ��������� FOV
    [SerializeField] private float normalHeight = 1.8f;      // ������ ������ ����
    [SerializeField] private float crouchHeight = 1f;        // ������ ������ ��� ����������
    [SerializeField] private float heightChangeSpeed = 5f;   // �������� ��������� ������

    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;        // ������������ �������
    [SerializeField] private float staminaDrainRate = 20f;   // �������� ������� �������
    [SerializeField] private float staminaRegenRate = 10f;   // �������� �������������� �������
    [SerializeField] private float staminaRegenDelay = 2f;   // �������� ����� ���������������

    [Header("Audio Settings")]
    [SerializeField] private AudioClip footstepSound;
    [SerializeField] private float walkStepInterval = 0.5f;
    [SerializeField] private float runStepInterval = 0.3f;
    [SerializeField] private float crouchStepInterval = 0.8f;

    [Header("Interaction Settings")]
    [SerializeField] private float interactionRange = 2f;    // ��������� ��������������
    [SerializeField] private LayerMask interactableLayer;    // ���� ��� ������������� ��������

    [Header("UI Settings")]
    [SerializeField] private Image staminaBar;               // ������ �������
    [SerializeField] private TextMeshProUGUI staminaText;    // ����� ��� �������� ������� (�����������)

    [Header("�������")]
    [SerializeField] private Flashlight flashlight;

    private CharacterController controller;                  // ������ �� CharacterController
    [SerializeField] private AudioSource audioSource;       // ��������� ��� ������������ ������
    private Vector3 velocity;                               // ������ �������� ��� ����������
    private bool isGrounded;                                // ��������� �� �������� �� �����
    private bool isCrouching;                               // ��������� �� ��������
    private float currentStamina;                           // ������� �������
    private float staminaTimer;                             // ������ ��� �������������� �������
    private float stepTimer;                                // ������ ��� �����
    private InventorySystem inventory;                      // ������ �� ������� ���������
    private float currentHeight;
    private Vector3 lastPosition;
    private float bobTimer;                                 // ������ ��� �����������
    private Vector3 baseCameraPosition;                     // ������� ������� ������

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        controller.enabled = false;

        // ������� ���� ��� ������ ���� �� ������
        if (cameraTarget == null)
        {
            GameObject targetObj = new GameObject("CameraTarget");
            cameraTarget = targetObj.transform;
            cameraTarget.SetParent(transform);
            cameraTarget.localPosition = new Vector3(0, normalHeight, 0);
            cameraTarget.localRotation = Quaternion.identity;
        }

        // ����������� ������ � ����
        if (playerCamera != null)
        {
            playerCamera.transform.SetParent(cameraTarget);
            playerCamera.transform.localPosition = Vector3.zero;
            playerCamera.transform.localRotation = Quaternion.identity;
        }

        currentHeight = normalHeight;
        controller.height = currentHeight;
        controller.center = new Vector3(0, currentHeight / 2f, 0);
        controller.enabled = true;
    }
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        currentStamina = maxStamina;
        inventory = GetComponent<InventorySystem>();
        stepTimer = walkStepInterval;
        lastPosition = transform.position;
        UpdateStaminaUI(); // ������������� UI
    }

    void Update()
    {
        HandleMovement();
        HandleCrouching();
        HandleJumping();
        HandleCamera();
        HandleStamina();
        HandleInteraction();
        HandleFootsteps();
        if (Input.GetKeyDown(KeyCode.E))
    {
            RaycastHit hit;
            if (Physics.Raycast(playerCamera.transform.position,
                               playerCamera.transform.forward,
                               out hit,
                               interactionRange))
            {
                if (hit.collider.CompareTag("Battery"))
                {
                    flashlight.RechargeBattery(30f);
                    Destroy(hit.collider.gameObject);
                }
            }
        }
    }

    public bool IsCrouching()
    {
        return isCrouching;
    }

    private void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;

        if (move.magnitude > 1f) move.Normalize();

        float currentSpeed = isCrouching ? crouchSpeed : (CanRun() ? runSpeed : walkSpeed);
        controller.Move(move * currentSpeed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    public bool CanRun()
    {
        return Input.GetKey(KeyCode.LeftShift) && currentStamina > 0 && !isCrouching;
    }

    private void HandleCrouching()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = !isCrouching;
        }

        // �������������� ����� �� ������� ��� ������
        if (isCrouching && Input.GetButtonDown("Jump"))
        {
            isCrouching = false;
        }

        float targetHeight = isCrouching ? crouchHeight : normalHeight;
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * heightChangeSpeed);

        controller.height = currentHeight;
        controller.center = new Vector3(0, currentHeight / 2f, 0);

        // ��������� ������� ���� ������
        cameraTarget.localPosition = new Vector3(0, currentHeight, 0);
    }

    private void HandleJumping()
    {
        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    private void HandleCamera()
    {
        float targetFOV = CanRun() ? runFOV : normalFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * fovChangeSpeed);
    }

    private void HandleStamina()
    {
        if (CanRun())
        {
            currentStamina = Mathf.Max(currentStamina - staminaDrainRate * Time.deltaTime, 0);
            staminaTimer = staminaRegenDelay;
        }
        else if (staminaTimer <= 0)
        {
            currentStamina = Mathf.Min(currentStamina + staminaRegenRate * Time.deltaTime, maxStamina);
        }
        else
        {
            staminaTimer -= Time.deltaTime;
        }

        UpdateStaminaUI(); // ��������� UI
    }

    private void HandleInteraction()
    {
        if (Input.GetKeyDown(KeyCode.E) && inventory != null)
        {
            RaycastHit hit;
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, interactionRange, interactableLayer))
            {
                if (hit.collider.CompareTag("Pickup"))
                {
                    string itemName = hit.collider.gameObject.name;
                    if (inventory.AddItem(itemName))
                    {
                        Destroy(hit.collider.gameObject);
                    }
                }
            }
        }
    }

    private void HandleFootsteps()
    {
        if (!isGrounded) return;

        float movedDistance = Vector3.Distance(transform.position, lastPosition);

        if (movedDistance > 0.01f)
        {
            stepTimer -= Time.deltaTime;

            if (stepTimer <= 0f)
            {
                if (footstepSound != null)
                {
                    audioSource.PlayOneShot(footstepSound);
                }

                if (isCrouching)
                    stepTimer = crouchStepInterval;
                else if (CanRun())
                    stepTimer = runStepInterval;
                else
                    stepTimer = walkStepInterval;
            }
        }

        lastPosition = transform.position;
    }

    

    private void UpdateStaminaUI()
    {
        if (staminaBar != null)
        {
            staminaBar.fillAmount = GetStaminaPercentage();
            // ��������� ����� ������ � ����������� �� ������ �������
            staminaBar.color = currentStamina < 20f ? Color.red : Color.green;
        }
        if (staminaText != null)
        {
            staminaText.text = $"Stamina: {(GetStaminaPercentage() * 100f):F0}%";
        }
    }

    public float GetStaminaPercentage()
    {
        return currentStamina / maxStamina;
    }
}