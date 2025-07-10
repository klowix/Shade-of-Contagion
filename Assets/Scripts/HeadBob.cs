using UnityEngine;

public class HeadBob : MonoBehaviour
{
    [Header("Параметры покачивания")]
    public float walkBobSpeed = 8f;
    public float walkBobAmount = 0.05f;
    public float runBobSpeed = 12f;
    public float runBobAmount = 0.1f;
    public float crouchBobSpeed = 4f;
    public float crouchBobAmount = 0.02f;

    [Header("Ссылки")]
    public CharacterController controller;
    public PlayerMovement playerMovement;

    private Vector3 initialPos;
    private float bobTimer;

    void Start()
    {
        // Используем локальную позицию относительно родителя (cameraTarget)
        initialPos = transform.localPosition;
    }

    void Update()
    {
        HandleHeadbob();
    }

    void HandleHeadbob()
    {
        if (controller == null || playerMovement == null) return;

        float xInput = Input.GetAxisRaw("Horizontal");
        float zInput = Input.GetAxisRaw("Vertical");
        bool isMoving = Mathf.Abs(xInput) > 0.1f || Mathf.Abs(zInput) > 0.1f;

        if (!controller.isGrounded || !isMoving)
        {
            // Возврат к базовой позиции
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                initialPos,
                Time.deltaTime * 6f
            );
            bobTimer = 0f;
            return;
        }

        float bobSpeed = walkBobSpeed;
        float bobAmount = walkBobAmount;

        if (playerMovement.IsCrouching())
        {
            bobSpeed = crouchBobSpeed;
            bobAmount = crouchBobAmount;
        }
        else if (playerMovement.CanRun())
        {
            bobSpeed = runBobSpeed;
            bobAmount = runBobAmount;
        }

        bobTimer += Time.deltaTime * bobSpeed;

        float bobX = Mathf.Cos(bobTimer) * bobAmount * 0.5f;
        float bobY = Mathf.Abs(Mathf.Sin(bobTimer)) * bobAmount;

        // Добавляем покачивание к начальной позиции
        transform.localPosition = initialPos + new Vector3(bobX, bobY, 0);
    }
}