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

    // --- Список для збереження створених підписів попиту ---
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
        // Періодично оновлюємо числа попиту як на панелі, так і над містами
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
        UpdateDemandWorldLabels(); // Показуємо попит над містами
    }

    public void ClosePanel()
    {
        if (panelObj != null) panelObj.SetActive(false);
        SelectedCity = null;
        ClearDemandWorldLabels(); // Приховуємо текст при закритті
    }

    private void RefreshUI()
    {
        if (SelectedCity == null) return;

        if (btnBuildWorkshop != null)
        {
            TextMeshProUGUI workshopBtnText = btnBuildWorkshop.GetComponentInChildren<TextMeshProUGUI>();

            if (SelectedCity.hasWorkshop)
            {
                if (workshopBtnText != null) workshopBtnText.text = "Майстерню";
                btnBuildWorkshop.interactable = true;
                btnBuildWorkshop.onClick.RemoveAllListeners();
                btnBuildWorkshop.onClick.AddListener(() =>
                {
                    if (WorkshopPanel.Instance != null) WorkshopPanel.Instance.OpenPanel(SelectedCity);
                });
            }
            else
            {
                if (workshopBtnText != null) workshopBtnText.text = "Майстерню";
                btnBuildWorkshop.interactable = true;
                btnBuildWorkshop.onClick.RemoveAllListeners();
                btnBuildWorkshop.onClick.AddListener(() =>
                {
                    SelectedCity.BuildWorkshop();
                    RefreshUI();
                });
            }
        }

        if (btnHireMechanic != null)
        {
            btnHireMechanic.interactable = SelectedCity.hasWorkshop;
            btnHireMechanic.onClick.RemoveAllListeners();
            btnHireMechanic.onClick.AddListener(() => { SelectedCity.HireMechanic(); RefreshUI(); });
        }

        if (btnCreateRoute != null)
        {
            btnCreateRoute.onClick.RemoveAllListeners();
            btnCreateRoute.onClick.AddListener(() => {
                ClosePanel(); // Закриваємо панель (що прибере і підписи над містами)
                if (RouteBuilderPanel.Instance != null) RouteBuilderPanel.Instance.OpenPanel(SelectedCity);
            });
        }

        if (btnBuildRoad != null)
        {
            btnBuildRoad.onClick.RemoveAllListeners();
            btnBuildRoad.onClick.AddListener(() => {
                ClosePanel();
                if (RoadBuilderPanel.Instance != null) RoadBuilderPanel.Instance.OpenPanel(SelectedCity);
            });
        }

        RefreshDemandsText();
    }

    private void RefreshDemandsText()
    {
        if (SelectedCity == null) return;
        string ds = "Доступно для перевезення:\n";
        if (SelectedCity.demands != null)
        {
            foreach (var d in SelectedCity.demands)
            {
                if (d == null || d.destination == null) continue;

                ds += $"<b>До: {d.destination.cityName}</b>\n";
                ds += $"  Вантажі: {d.currentCargo} / {d.maxCargo}\n";
                ds += $"  Пасажири: {d.currentPassengers} / {d.maxPassengers}\n";
            }
        }

        if (demandsListText != null) demandsListText.text = ds;
    }

    // --- ЛОГІКА ДИНАМІЧНОГО ТЕКСТУ НАД МІСТАМИ ---

    private void UpdateDemandWorldLabels()
    {
        if (SelectedCity == null || SelectedCity.demands == null)
        {
            ClearDemandWorldLabels();
            lastLabelCity = null;
            return;
        }

        // Якщо змінили вибране місто АБО кількість міст-призначень змінилася, перестворюємо об'єкти
        if (demandWorldLabels.Count != SelectedCity.demands.Count || lastLabelCity != SelectedCity)
        {
            ClearDemandWorldLabels();
            lastLabelCity = SelectedCity;

            for (int i = 0; i < SelectedCity.demands.Count; i++)
            {
                var d = SelectedCity.demands[i];
                if (d == null || d.destination == null) continue;

                // Створюємо порожній об'єкт і вішаємо на нього TextMeshPro
                GameObject labelObj = new GameObject($"DemandLabel_{d.destination.cityName}");
                // Встановлюємо позицію вище за місто призначення
                labelObj.transform.position = d.destination.transform.position + new Vector3(0, 1.2f, 0);

                var tmp = labelObj.AddComponent<TextMeshPro>();
                tmp.fontSize = 2.5f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.sortingOrder = 15; // Щоб текст перекривав дороги та інше

                demandWorldLabels.Add(labelObj);
            }
        }

        // Оновлюємо числа, їхній колір та позицію
        for (int i = 0; i < demandWorldLabels.Count; i++)
        {
            if (i >= SelectedCity.demands.Count) break;
            var d = SelectedCity.demands[i];
            var obj = demandWorldLabels[i];
            if (obj == null || d == null || d.destination == null) continue;

            // !!ВАЖЛИВО!!: Оновлюємо позицію щоб текст завжди був над потрібним містом 
            obj.transform.position = d.destination.transform.position + new Vector3(0, 1.2f, 0);

            var tmp = obj.GetComponent<TextMeshPro>();
            if (tmp != null)
            {
                // В жовтому кольорі вантажі, в блакитному — пасажири
                tmp.text = $"<b><color=#FFDB58>В: {d.currentCargo}</color> | <color=#00BFFF>П: {d.currentPassengers}</color></b>";
            }
        }
    }

    private void ClearDemandWorldLabels()
    {
        foreach (var obj in demandWorldLabels)
        {
            if (obj != null) Destroy(obj);
        }
        demandWorldLabels.Clear();
    }
}