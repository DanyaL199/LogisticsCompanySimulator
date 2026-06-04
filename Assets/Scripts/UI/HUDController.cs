using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [Header("Текстові поля")]
    public TextMeshProUGUI balanceText;
    public TextMeshProUGUI dateText;

    [Header("Кнопки швидкості")]
    public Button btnPause;
    public Button btn1x;
    public Button btn2x;
    public Button btn4x;

    [Header("Кольори кнопок")]
    public Color colorActive = new Color(1f, 0.6f, 0f, 1f);
    public Color colorInactive = new Color(0.2f, 0.2f, 0.2f, 0.8f);

    private void Start()
    {

        btnPause.onClick.AddListener(() => SetSpeed(0));
        btn1x.onClick.AddListener(() => SetSpeed(1));
        btn2x.onClick.AddListener(() => SetSpeed(2));
        btn4x.onClick.AddListener(() => SetSpeed(3));

        if (GameTimeManager.Instance != null)
            GameTimeManager.Instance.OnHourChanged += OnHourChanged;

        UpdateBalance();
        UpdateSpeedButtons();
    }

    private void OnDestroy()
    {
        if (GameTimeManager.Instance != null)
            GameTimeManager.Instance.OnHourChanged -= OnHourChanged;
    }

    private void Update()
    {
        UpdateBalance();
    }

    private void OnHourChanged(GameDate date)
    {
        if (dateText != null)
            dateText.text = date.ToString();
    }

    private void UpdateBalance()
    {
        if (balanceText == null || FinanceManager.Instance == null) return;

        float balance = FinanceManager.Instance.balance;
        balanceText.text = FormatMoney(balance) + " грн";
        balanceText.color = balance >= 0
            ? new Color(1f, 0.85f, 0f)   
            : new Color(1f, 0.2f, 0.2f); 
    }

    private void SetSpeed(int speed)
    {
        GameTimeManager.Instance?.SetSpeed(speed);
        UpdateSpeedButtons();
    }

    private void UpdateSpeedButtons()
    {
        if (GameTimeManager.Instance == null) return;
        int current = GameTimeManager.Instance.timeSpeed;

        SetButtonColor(btnPause, current == 0);
        SetButtonColor(btn1x, current == 1);
        SetButtonColor(btn2x, current == 2);
        SetButtonColor(btn4x, current == 4);
    }

    private void SetButtonColor(Button btn, bool isActive)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null)
            img.color = isActive ? colorActive : colorInactive;
    }

    private string FormatMoney(float value)
    {
        bool negative = value < 0;
        string s = Mathf.Abs(value).ToString("F0");

        string result = "";
        int count = 0;
        for (int i = s.Length - 1; i >= 0; i--)
        {
            if (count > 0 && count % 3 == 0)
                result = " " + result;
            result = s[i] + result;
            count++;
        }

        return negative ? "-" + result : result;
    }
}