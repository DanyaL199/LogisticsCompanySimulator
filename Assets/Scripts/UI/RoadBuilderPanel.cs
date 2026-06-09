using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

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

    private const float KM_PER_UNIT = 60f;

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
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                OnCancel();
                return;
            }

            var selected = MapClickHandler.Instance?.GetSelectedCities();
            if (selected != null)
            {
                List<CityNode> virtualSelected = new List<CityNode>();
                if (cityA != null && !selected.Contains(cityA))
                {
                    virtualSelected.Add(cityA);
                }
                virtualSelected.AddRange(selected);

                if (virtualSelected.Count > 0)
                {
                    if (cityA != virtualSelected[0])
                    {
                        cityA = virtualSelected[0];
                        RefreshInfo();
                    }

                    if (virtualSelected.Count >= 2 && cityB != virtualSelected[1])
                    {
                        cityB = virtualSelected[1];
                        RefreshInfo();
                    }

                    if (virtualSelected.Count > 2)
                    {
                        if (selected.Count > 0)
                            MapClickHandler.Instance.RemoveCityExternal(selected[selected.Count - 1], new List<CityNode> { cityA, cityB });
                    }
                    else if (virtualSelected.Count == 1 && cityB != null)
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
        List<TMP_Dropdown.OptionData> opts = new List<TMP_Dropdown.OptionData>();

        foreach (var rt in RoadNetwork.Instance.availableRoadTypes)
        {
            opts.Add(new TMP_Dropdown.OptionData($"{rt.roadName} (Шв: {rt.speedLimitKmh}, Варт: {rt.buildCost})"));
        }
        roadTypeDropdown.AddOptions(opts);
        roadTypeDropdown.value = 0;
    }

    private void RefreshInfo()
    {
        if (cityAText != null) cityAText.text = cityA != null ? $"1. {cityA.cityName}" : "1. Оберіть місто А";
        if (cityBText != null) cityBText.text = cityB != null ? $"2. {cityB.cityName}" : "2. Оберіть місто Б";

        if (cityA == null || cityB == null)
        {
            if (distanceText != null) distanceText.text = "Відстань: —";
            btnConfirm.interactable = false;
            SetWarning("Виберіть два міста на карті.");
            return;
        }

        if (cityA == cityB)
        {
            if (distanceText != null) distanceText.text = "Відстань: —";
            btnConfirm.interactable = false;
            SetWarning("Виберіть різні міста.");
            return;
        }

        float distKm = Vector3.Distance(cityA.transform.position, cityB.transform.position) * KM_PER_UNIT;
        if (distanceText != null) distanceText.text = $"Відстань: {distKm:F0} км";

        btnConfirm.interactable = true;
        SetWarning("");

        int idx = roadTypeDropdown.value;
        if (idx < 0 || idx >= RoadNetwork.Instance.availableRoadTypes.Count) return;

        RoadData selectedRoad = RoadNetwork.Instance.availableRoadTypes[idx];
        RoadConnection existing = RoadNetwork.Instance.GetRoad(cityA, cityB);

        float costMultiplier = distKm / 10f;
        float cost = selectedRoad.buildCost * costMultiplier;

        if (existing != null)
        {
            if (existing.roadData.roadType >= selectedRoad.roadType)
            {
                SetWarning("Вже існує дорога такого або вищого рівня.");
                btnConfirm.interactable = false;
                return;
            }
            float diff = selectedRoad.buildCost - existing.roadData.buildCost;
            cost = diff > 0 ? (diff * costMultiplier) : 0;
            SetWarning($"Покращення дороги. Вартість: {cost:F0} грн");
        }
        else
        {
            SetWarning($"Будівництво нової дороги. Вартість: {cost:F0} грн");
        }

        if (FinanceManager.Instance != null && !FinanceManager.Instance.CanAfford(cost))
        {
            SetWarning($"Недостатньо коштів! Вартість: {cost:F0} грн");
            btnConfirm.interactable = false;
            return;
        }
    }

    private void SetWarning(string msg)
    {
        if (warningText != null) warningText.text = msg;
    }

    public void OnConfirm()
    {
        if (cityA == null || cityB == null) return;
        int idx = roadTypeDropdown.value;
        if (idx < 0 || idx >= RoadNetwork.Instance.availableRoadTypes.Count) return;

        RoadData selectedRoad = RoadNetwork.Instance.availableRoadTypes[idx];

        RoadConnection existing = RoadNetwork.Instance.GetRoad(cityA, cityB);

        float distKm = Vector3.Distance(cityA.transform.position, cityB.transform.position) * KM_PER_UNIT;
        float costMultiplier = distKm / 10f;
        float cost = selectedRoad.buildCost * costMultiplier;

        if (existing != null)
        {
            float diff = selectedRoad.buildCost - existing.roadData.buildCost;
            cost = diff > 0 ? (diff * costMultiplier) : 0;
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

    public void OnCancel()
    {
        ClosePanel();
    }
}