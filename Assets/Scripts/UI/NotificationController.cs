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

        if (notificationText != null)
            notificationText.text = "";

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private void Start()
    {
    }

    private void OnDestroy()
    {
    }

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

    public static void ShowMessage(string msg, Color color, float duration = 4f)
    {
        Instance?.Show(msg, color, duration);
    }
}