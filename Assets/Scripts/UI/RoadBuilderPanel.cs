using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class RoadBuilderPanel : MonoBehaviour
{
    public static RoadBuilderPanel Instance { get; private set; }

    [Header("Кореневий об'єкт панелі")]
    public GameObject panelRoot;

    [Header("Рядки міст")]
    public TextMeshProUGUI cityAText;
    public TextMeshProUGUI cityBText;

    [Header("Текст відстані")]
    public TextMeshProUGUI distanceText;

    [Header("Текст попередження/вартості")]
    public TextMeshProUGUI warningText;

    [Header("Вибір дороги")]
    public TMP_Dropdown roadTypeDropdown;

    [Header("Кнопки")]
    public Button btnConfirm;
    public Button btnCancel;
    public Button btnOpenPanel; // Кнопка верхньої панелі

    private CityNode cityA;
    private CityNode cityB;
    private const float KM_PER_UNIT = 50f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        btnOpenPanel?.onClick.AddListener(() => OpenPanel(null));
        btnConfirm?.onClick.AddListener(OnConfirm);
        btnCancel?.onClick.AddListener(OnCancel);
        roadTypeDropdown?.onValueChanged.AddListener((_) => RefreshInfo());

        panelRoot?.SetActive(false);
        SetWarning("");
    }

    private void Update()
    {
        if (panelRoot != null && panelRoot.activeSelf)
        {
            // Якщо вибрано більше 2 міст, обрізаємо
            var selected = MapClickHandler.Instance?.GetSelectedCities();
            if (selected != null && selected.Count > 0)
            {
                if (cityA != selected[0])
                {
                    cityA = selected[0];
                    RefreshInfo();
                }

                if (selected.Count == 2 && cityB != selected[1])
                {
                    cityB = selected[1];
                    RefreshInfo();
                }
                else if (selected.Count > 2)
                {
                    // Скасовуємо третє виділене місто
                    MapClickHandler.Instance.RemoveCityExternal(selected[2], new List<CityNode> { cityA, cityB });
                }
                else if (selected.Count == 1 && cityB != null)
                {
                    cityB = null;
                    RefreshInfo();
                }
            }
            else if (cityA != null || cityB != null)
            {
                cityA = cityB = null;
                RefreshInfo();
            }
        }
    }

    public void OpenPanel(CityNode startCity = null)
    {
        cityA = startCity;
        cityB = null;

        SetWarning("");
        PopulateDropdown();
        panelRoot?.SetActive(true);
        MapClickHandler.Instance?.StartBuilding(startCity);

        RefreshInfo();
    }

    private void ClosePanel()
    {
        panelRoot?.SetActive(false);
        cityA = cityB = null;
        MapClickHandler.Instance?.StopBuilding();
    }

    private void PopulateDropdown()
    {
        if (roadTypeDropdown == null || RoadNetwork.Instance == null) return;
        roadTypeDropdown.ClearOptions();

        var options = new List<string>();
        foreach (var rt in RoadNetwork.Instance.availableRoadTypes)
        {
            if (rt != null) options.Add($"{rt.roadName} ({rt.buildCost} у.о.)");
        }
        roadTypeDropdown.AddOptions(options);
    }

    private void RefreshInfo()
    {
        if (cityAText != null) cityAText.text = cityA ? $"1. {cityA.cityName}" : "1. Оберіть місто А";
        if (cityBText != null) cityBText.text = cityB ? $"2. {cityB.cityName}" : "2. Оберіть місто Б";

        if (cityA == null || cityB == null)
        {
            if (distanceText != null) distanceText.text = "Відстань: —";
            btnConfirm.interactable = false;
            SetWarning("");
            return;
        }

        float totalKm = Vector3.Distance(cityA.transform.position, cityB.transform.position) * KM_PER_UNIT;
        if (distanceText != null) distanceText.text = $"Відстань: {totalKm:F0} км";

        ValidateRoad();
    }

    private void ValidateRoad()
    {
        if (RoadNetwork.Instance == null || RoadNetwork.Instance.availableRoadTypes.Count == 0) return;
        if (roadTypeDropdown == null) return;

        int idx = roadTypeDropdown.value;
        if (idx < 0 || idx >= RoadNetwork.Instance.availableRoadTypes.Count) return;

        RoadData selectedRoad = RoadNetwork.Instance.availableRoadTypes[idx];
        RoadConnection existing = RoadNetwork.Instance.GetRoad(cityA, cityB);

        float cost = selectedRoad.buildCost;

        if (existing != null)
        {
            if (existing.roadData.roadType >= selectedRoad.roadType)
            {
                SetWarning("Вже існує дорога такого або вищого рівня.");
                btnConfirm.interactable = false;
                return;
            }
            float diff = selectedRoad.buildCost - existing.roadData.buildCost;
            cost = diff > 0 ? diff : 0;
            SetWarning($"Покращення дороги. Вартість: {cost} у.о.");
        }
        else
        {
            SetWarning($"Будівництво нової дороги. Вартість: {cost} у.о.");
        }

        if (FinanceManager.Instance != null && !FinanceManager.Instance.CanAfford(cost))
        {
            SetWarning($"Недостатньо коштів! Вартість: {cost} у.о.");
            btnConfirm.interactable = false;
            return;
        }

        btnConfirm.interactable = true;
    }

    private void OnConfirm()
    {
        if (cityA == null || cityB == null) return;

        int idx = roadTypeDropdown.value;
        if (idx < 0 || idx >= RoadNetwork.Instance.availableRoadTypes.Count) return;

        RoadData selectedRoad = RoadNetwork.Instance.availableRoadTypes[idx];

        RoadConnection existing = RoadNetwork.Instance.GetRoad(cityA, cityB);
        float cost = selectedRoad.buildCost;
        if (existing != null)
        {
            float diff = selectedRoad.buildCost - existing.roadData.buildCost;
            cost = diff > 0 ? diff : 0;
        }

        if (FinanceManager.Instance != null && FinanceManager.Instance.CanAfford(cost))
        {
            bool success = RoadNetwork.Instance.BuildRoad(cityA, cityB, selectedRoad);
            if (success)
            {
                FinanceManager.Instance.AddExpense(cost);
                ClosePanel();
            }
        }
    }

    private void OnCancel() => ClosePanel();

    private void SetWarning(string msg)
    {
        if (warningText == null) return;
        warningText.text = msg;
        warningText.gameObject.SetActive(!string.IsNullOrEmpty(msg));
    }
}