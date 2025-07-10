using UnityEngine;
using UnityEngine.UI; // Добавлено для UI
using TMPro; // Добавлено для TextMeshPro (опционально)

[RequireComponent(typeof(CharacterController), typeof(AudioSource))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 12f;          // Скорость ходьбы
    [SerializeField] private float runSpeed = 18f;           // Скорость бега
    [SerializeField] private float crouchSpeed = 6f;         // Скорость приседания
    [SerializeField] private float gravity = -9.81f;         // Сила гравитации
    [SerializeField] private float jumpHeight = 3f;          // Высота прыжка

    [Header("Camera Settings")]
    [SerializeField] private Camera playerCamera;            // Ссылка на камеру
    [SerializeField] private Transform cameraTarget; // Добавляем эту строку
    [SerializeField] private float normalFOV = 60f;          // Нормальное поле зрения
    [SerializeField] private float runFOV = 70f;             // Поле зрения при беге
    [SerializeField] private float fovChangeSpeed = 5f;      // Скорость изменения FOV
    [SerializeField] private float normalHeight = 1.8f;      // Высота камеры стоя
    [SerializeField] private float crouchHeight = 1f;        // Высота камеры при приседании
    [SerializeField] private float heightChangeSpeed = 5f;   // Скорость изменения высоты

    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;        // Максимальная стамина
    [SerializeField] private float staminaDrainRate = 20f;   // Скорость расхода стамины
    [SerializeField] private float staminaRegenRate = 10f;   // Скорость восстановления стамины
    [SerializeField] private float staminaRegenDelay = 2f;   // Задержка перед восстановлением

    [Header("Audio Settings")]
    [SerializeField] private AudioClip footstepSound;
    [SerializeField] private float walkStepInterval = 0.5f;
    [SerializeField] private float runStepInterval = 0.3f;
    [SerializeField] private float crouchStepInterval = 0.8f;

    [Header("Interaction Settings")]
    [SerializeField] private float interactionRange = 2f;    // Дальность взаимодействия
    [SerializeField] private LayerMask interactableLayer;    // Слой для интерактивных объектов

    [Header("UI Settings")]
    [SerializeField] private Image staminaBar;               // Полоса стамины
    [SerializeField] private TextMeshProUGUI staminaText;    // Текст для процента стамины (опционально)

    [Header("Фонарик")]
    [SerializeField] private Flashlight flashlight;

    private CharacterController controller;                  // Ссылка на CharacterController
    [SerializeField] private AudioSource audioSource;       // Компонент для проигрывания звуков
    private Vector3 velocity;                               // Вектор скорости для гравитации
    private bool isGrounded;                                // Находится ли персонаж на земле
    private bool isCrouching;                               // Приседает ли персонаж
    private float currentStamina;                           // Текущая стамина
    private float staminaTimer;                             // Таймер для восстановления стамины
    private float stepTimer;                                // Таймер для шагов
    private InventorySystem inventory;                      // Ссылка на систему инвентаря
    private float currentHeight;
    private Vector3 lastPosition;
    private float bobTimer;                                 // Таймер для покачивания
    private Vector3 baseCameraPosition;                     // Базовая позиция камеры

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        controller.enabled = false;

        // Создаем цель для камеры если не задана
        if (cameraTarget == null)
        {
            GameObject targetObj = new GameObject("CameraTarget");
            cameraTarget = targetObj.transform;
            cameraTarget.SetParent(transform);
            cameraTarget.localPosition = new Vector3(0, normalHeight, 0);
            cameraTarget.localRotation = Quaternion.identity;
        }

        // Привязываем камеру к цели
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
        UpdateStaminaUI(); // Инициализация UI
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

        // Автоматический выход из приседа при прыжке
        if (isCrouching && Input.GetButtonDown("Jump"))
        {
            isCrouching = false;
        }

        float targetHeight = isCrouching ? crouchHeight : normalHeight;
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * heightChangeSpeed);

        controller.height = currentHeight;
        controller.center = new Vector3(0, currentHeight / 2f, 0);

        // Обновляем позицию цели камеры
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

        UpdateStaminaUI(); // Обновляем UI
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
            // Изменение цвета полосы в зависимости от уровня стамины
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