using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class CityInfoPanel : MonoBehaviour
{
    public static CityInfoPanel Instance { get; private set; }

    public GameObject panelObj;
    public TextMeshProUGUI cityNameText;
    public TextMeshProUGUI demandsListText;

    public Button btnBuildWorkshop;
    public Button btnHireMechanic;
    public Button btnCreateRoute;
    public Button btnBuildRoad;
    public Button btnClose;

    public CityNode SelectedCity { get; private set; }

    // --- Додано: для зберігання підписів попиту над містами ---
    private List<GameObject> demandWorldLabels = new List<GameObject>();
    private float updateTimer = 0f;
    private CityNode lastLabelCity = null;

    private void Awake()
    {
        Instance = this;
        if (panelObj != null) panelObj.SetActive(false);
        if (btnClose != null) btnClose.onClick.AddListener(ClosePanel);
    }

    private void Update()
    {
        // Оновлюємо інформацію про попит кожні 0.2 сек, щоб не вантажити систему
        if (panelObj != null && panelObj.activeSelf && SelectedCity != null)
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= 0.2f)
            {
                updateTimer = 0f;
                RefreshDemandsText();
                UpdateDemandWorldLabels();
            }
        }
    }

    public void OpenPanel(CityNode city)
    {
        SelectedCity = city;
        if (cityNameText != null) cityNameText.text = city.cityName;
        if (panelObj != null) panelObj.SetActive(true);
        RefreshUI();
    }

    public void ClosePanel()
    {
        if (panelObj != null) panelObj.SetActive(false);
        SelectedCity = null;
        ClearDemandWorldLabels(); // Ховаємо підписи при закритті
    }

    private void RefreshUI()
    {
        if (SelectedCity == null) return;

        // Майстерня
        if (SelectedCity.hasWorkshop)
        {
            if (btnBuildWorkshop != null)
            {
                btnBuildWorkshop.GetComponentInChildren<TextMeshProUGUI>().text = "Відкрити Майстерню";
                btnBuildWorkshop.onClick.RemoveAllListeners();
                btnBuildWorkshop.onClick.AddListener(() => {
                    ClosePanel();
                    if (WorkshopPanel.Instance != null) WorkshopPanel.Instance.OpenPanel(SelectedCity);
                });
            }
            if (btnHireMechanic != null) btnHireMechanic.gameObject.SetActive(false);
        }
        else
        {
            if (btnBuildWorkshop != null)
            {
                btnBuildWorkshop.GetComponentInChildren<TextMeshProUGUI>().text = "Збудувати Майстерню (10 000 у.о.)";
                btnBuildWorkshop.onClick.RemoveAllListeners();
                btnBuildWorkshop.onClick.AddListener(() => {
                    if (FinanceManager.Instance != null && FinanceManager.Instance.CanAfford(10000))
                    {
                        FinanceManager.Instance.AddExpense(10000);
                        SelectedCity.BuildWorkshop();
                        RefreshUI();
                    }
                });
            }
            if (btnHireMechanic != null) btnHireMechanic.gameObject.SetActive(false);
        }

        // Кнопки маршрутів та доріг
        if (btnCreateRoute != null)
        {
            btnCreateRoute.onClick.RemoveAllListeners();
            btnCreateRoute.onClick.AddListener(() => {
                CityNode cityToPass = SelectedCity;
                ClosePanel(); // Закриваємо панель (що прибере і підписи над містами)
                if (RouteBuilderPanel.Instance != null) RouteBuilderPanel.Instance.OpenPanel(cityToPass);
            });
        }

        if (btnBuildRoad != null)
        {
            btnBuildRoad.onClick.RemoveAllListeners();
            btnBuildRoad.onClick.AddListener(() => {
                CityNode cityToPass = SelectedCity;
                ClosePanel();
                if (RoadBuilderPanel.Instance != null) RoadBuilderPanel.Instance.OpenPanel(cityToPass);
            });
        }

        RefreshDemandsText();
        UpdateDemandWorldLabels(); // Показуємо/оновлюємо підписи попиту на самій карті
    }

    private void RefreshDemandsText()
    {
        if (demandsListText == null || SelectedCity == null || SelectedCity.demands == null) return;
        string txt = "Попит:\n";
        foreach (var d in SelectedCity.demands)
        {
            if (d.destination != null && d.currentCargo > 0)
            {
                txt += $"- до {d.destination.cityName}: {Mathf.FloorToInt(d.currentCargo)}/{d.maxCargo} кг\n";
            }
        }
        demandsListText.text = txt;
    }

    private void UpdateDemandWorldLabels()
    {
        if (SelectedCity == null || SelectedCity.demands == null) return;

        // Якщо обрали інше місто - чистимо старі підписи
        if (lastLabelCity != SelectedCity)
        {
            ClearDemandWorldLabels();
            lastLabelCity = SelectedCity;
        }

        int i = 0;
        foreach (var d in SelectedCity.demands)
        {
            if (d.destination != null && d.currentCargo > 0)
            {
                // Якщо не вистачає плашок - створюємо нову
                if (i >= demandWorldLabels.Count)
                {
                    demandWorldLabels.Add(CreateDemandLabel());
                }

                GameObject labelObj = demandWorldLabels[i];
                labelObj.SetActive(true);

                // Ставимо плашку над містом-призначенням
                // (або збоку від нього, щоб не перекривати)
                labelObj.transform.position = d.destination.transform.position + new Vector3(0, 0.8f, 0);

                var tmp = labelObj.GetComponent<TextMeshPro>();
                if (tmp != null)
                {
                    // Вантаж: В | Пасажири: П (поки що просто вантаж)
                    tmp.text = $"<color=#FFAA00>В: {Mathf.FloorToInt(d.currentCargo)}</color> | <color=#00AAFF>П: {Mathf.FloorToInt(d.currentPassengers)}</color>";
                }

                i++;
            }
        }

        // Ховаємо зайві плашки
        for (; i < demandWorldLabels.Count; i++)
        {
            demandWorldLabels[i].SetActive(false);
        }
    }

    private void ClearDemandWorldLabels()
    {
        foreach (var lbl in demandWorldLabels)
        {
            if (lbl != null) Destroy(lbl);
        }
        demandWorldLabels.Clear();
    }

    private GameObject CreateDemandLabel()
    {
        GameObject obj = new GameObject("DemandLabel");

        // Робимо його дитиною нашого UI або просто в корні сцени
        // Оскільки це TextMeshPro (не UGUI), він житиме у світових координатах

        var tmp = obj.AddComponent<TextMeshPro>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 2.5f;
        tmp.color = Color.white;

        // Можна додати обведення для кращої видимості на фоні трави
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = Color.black;

        // Щоб рендерилося поверх усього (опціонально)
        tmp.sortingOrder = 50;

        return obj;
    }
}