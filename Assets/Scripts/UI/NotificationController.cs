using UnityEngine;
using TMPro;
using DG.Tweening;

public class NotificationController : MonoBehaviour
{
    public static NotificationController Instance { get; private set; }

    [Header("Елементи UI")]
    public TextMeshProUGUI notificationText;
    public CanvasGroup canvasGroup;

    private Sequence currentSequence;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private void Start()
    {
        // Підписатись на страйк
        if (WageManager.Instance != null)
        {
            WageManager.Instance.OnWarningTriggered += OnWageWarning;
            WageManager.Instance.OnStrikeStarted += OnStrike;
            WageManager.Instance.OnStrikeEnded += OnStrikeEnded;
        }
    }

    private void OnDestroy()
    {
        if (WageManager.Instance != null)
        {
            WageManager.Instance.OnWarningTriggered -= OnWageWarning;
            WageManager.Instance.OnStrikeStarted -= OnStrike;
            WageManager.Instance.OnStrikeEnded -= OnStrikeEnded;
        }
    }

    // Показати сповіщення
    public void Show(string message, Color color, float duration = 4f)
    {
        if (notificationText == null || canvasGroup == null) return;

        currentSequence?.Kill();

        notificationText.text = message;
        notificationText.color = color;

        currentSequence = DOTween.Sequence();
        currentSequence.Append(canvasGroup.DOFade(1f, 0.3f));
        currentSequence.AppendInterval(duration);
        currentSequence.Append(canvasGroup.DOFade(0f, 0.5f));
    }

    private void OnWageWarning()
    {
        Show("⚠ Водії незадоволені зарплатою!\nЄ 1 місяць до страйку.",
             new Color(1f, 0.7f, 0f), 6f);
    }

    private void OnStrike()
    {
        Show("🚫 СТРАЙК! Всі транспортні засоби зупинені.",
             new Color(1f, 0.2f, 0.2f), 8f);
    }

    private void OnStrikeEnded()
    {
        Show("✅ Страйк завершено. Маршрути відновлено.",
             new Color(0.2f, 1f, 0.4f), 4f);
    }

    // Виклик з будь-якого місця
    public static void ShowMessage(string msg, Color color, float duration = 4f)
    {
        Instance?.Show(msg, color, duration);
    }
}