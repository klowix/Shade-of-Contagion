using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Flashlight : MonoBehaviour
{
    [Header("Основные настройки")]
    [SerializeField] private Light flashlightLight;
    [SerializeField] private KeyCode toggleKey = KeyCode.F;
    [SerializeField] private AudioClip flashOnSound;  // Звук включения
    [SerializeField] private AudioClip flashOffSound; // Звук выключения
    [SerializeField] private float maxIntensity = 2f;
    [SerializeField] private float lightChangeSpeed = 5f;
    [SerializeField] private Vector3 positionOffset = new Vector3(0.2f, -0.1f, 0.1f);
    [SerializeField] private Vector3 rotationOffset = new Vector3(5f, 0, 0);

    [Header("Эффекты")]
    [SerializeField] private bool enableShake = true;
    [SerializeField] private float shakeIntensity = 0.01f;
    [SerializeField] private float shakeSpeed = 3f;

    [Header("Батарея")]
    [SerializeField] private bool useBattery = true;
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float batteryDrainRate = 5f;
    [SerializeField] private float batteryRegenRate = 2f;
    [SerializeField] private float batteryRegenDelay = 3f;

    [Header("UI")]
    [SerializeField] private Image batteryBar;
    [SerializeField] private Color fullBatteryColor = Color.green;
    [SerializeField] private Color lowBatteryColor = Color.red;
    
    [Header("UI Text")]
    [SerializeField] private TextMeshProUGUI batteryText; // Для TextMeshPro

    private AudioSource audioSource;
    private bool isOn;
    private float targetIntensity;
    private float currentBattery;
    private float batteryTimer;
    private Vector3 originalPosition;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0.7f;
        }

        // Настройка фонарика
        if (flashlightLight != null)
        {
            flashlightLight.enabled = true;
            flashlightLight.intensity = 0;
            targetIntensity = 0;

            // Привязка к камере
            flashlightLight.transform.SetParent(Camera.main.transform);
            flashlightLight.transform.localPosition = positionOffset;
            flashlightLight.transform.localRotation = Quaternion.Euler(rotationOffset);
            originalPosition = flashlightLight.transform.localPosition;
        }

        currentBattery = maxBattery;
        UpdateBatteryUI();
    }

    void Update()
    {
        HandleInput();
        UpdateLight();
        UpdateBattery();
        UpdateShake();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleFlashlight();
        }
    }

    public void ToggleFlashlight()
    {
        if (useBattery && currentBattery <= 0) return;

        isOn = !isOn;
        targetIntensity = isOn ? maxIntensity : 0f;

        // Воспроизведение соответствующего звука
        if (isOn && flashOnSound != null)
        {
            audioSource.PlayOneShot(flashOnSound);
        }
        else if (!isOn && flashOffSound != null)
        {
            audioSource.PlayOneShot(flashOffSound);
        }
    }

    private void UpdateLight()
    {
        if (flashlightLight == null) return;

        // Плавное изменение интенсивности
        flashlightLight.intensity = Mathf.Lerp(
            flashlightLight.intensity,
            targetIntensity,
            Time.deltaTime * lightChangeSpeed
        );

        // Мерцание при низком заряде
        if (isOn && useBattery && currentBattery < 20f)
        {
            float flicker = Mathf.PerlinNoise(Time.time * 10f, 0) * 0.5f;
            flashlightLight.intensity = Mathf.Clamp(
                flashlightLight.intensity + flicker,
                0.1f,
                maxIntensity
            );
        }
    }

    private void UpdateBattery()
    {
        if (!useBattery) return;

        if (isOn)
        {
            currentBattery -= batteryDrainRate * Time.deltaTime;
            currentBattery = Mathf.Max(currentBattery, 0);
            batteryTimer = batteryRegenDelay;

            // Автовыключение
            if (currentBattery <= 0)
            {
                isOn = false;
                targetIntensity = 0f;

                // Воспроизведение звука выключения при разряде
                if (flashOffSound != null)
                {
                    audioSource.PlayOneShot(flashOffSound);
                }
            }
            
        }
        else if (batteryTimer <= 0)
        {
            currentBattery = Mathf.Min(
                currentBattery + batteryRegenRate * Time.deltaTime,
                maxBattery
            );
        }
        else
        {
            batteryTimer -= Time.deltaTime;
        }

        UpdateBatteryUI();
    }

    private void UpdateShake()
    {
        if (!enableShake || !isOn || flashlightLight == null) return;

        // Дрожание фонарика
        float offsetX = Mathf.PerlinNoise(Time.time * shakeSpeed, 0) * 2f - 1f;
        float offsetY = Mathf.PerlinNoise(0, Time.time * shakeSpeed) * 2f - 1f;
        float offsetZ = Mathf.PerlinNoise(Time.time * shakeSpeed, Time.time * shakeSpeed) * 2f - 1f;

        Vector3 shakeOffset = new Vector3(
            offsetX * shakeIntensity,
            offsetY * shakeIntensity,
            offsetZ * shakeIntensity
        );

        flashlightLight.transform.localPosition = originalPosition + shakeOffset;
    }

    private void UpdateBatteryUI()
    {
        if (batteryBar == null) return;

        float batteryPercent = currentBattery / maxBattery;
        batteryBar.fillAmount = batteryPercent;
        batteryBar.color = Color.Lerp(
            lowBatteryColor,
            fullBatteryColor,
            batteryPercent
        );
        if (batteryText != null)
        {
            batteryText.text = $"{Mathf.RoundToInt(batteryPercent * 100)}%";
        }
        if (currentBattery < 20f)
        {
            float alpha = Mathf.PingPong(Time.time * 2f, 1f);
            batteryBar.color = new Color(
                batteryBar.color.r,
                batteryBar.color.g,
                batteryBar.color.b,
                alpha
            );
        }

    }

    // Публичные методы для взаимодействия
    public void RechargeBattery(float amount)
    {
        currentBattery = Mathf.Min(currentBattery + amount, maxBattery);
        UpdateBatteryUI();
    }

    public bool IsActive()
    {
        return isOn;
    }
}