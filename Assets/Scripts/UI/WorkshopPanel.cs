using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class WorkshopPanel : MonoBehaviour
{
    public static WorkshopPanel Instance { get; private set; }

    public GameObject panelObj;
    public TextMeshProUGUI headerText;
    public TextMeshProUGUI workshopInfoText;
    public Transform contentTransform;
    public Button btnClose;
    public Button btnHireMechanic;
    public Button btnUpgradeWorkshop;

    private CityNode currentWorkshopCity;
    private List<GameObject> activeRows = new List<GameObject>();

    private void Awake() { if (Instance == null) Instance = this; if (btnClose != null) btnClose.onClick.AddListener(ClosePanel); if (panelObj != null) panelObj.SetActive(false); }
    public void OpenPanel(CityNode city) { currentWorkshopCity = city; if (panelObj != null) panelObj.SetActive(true); RefreshWorkshopInfo(); UpdateRows(); }
    public void ClosePanel() { if (panelObj != null) panelObj.SetActive(false); currentWorkshopCity = null; }

    private void RefreshWorkshopInfo()
    {
        if (currentWorkshopCity == null) return;
        if (headerText != null) headerText.text = $"Майстерня: {currentWorkshopCity.cityName}";
        if (workshopInfoText != null) workshopInfoText.text = $"Слоти: {currentWorkshopCity.repairSlots}/{CityNode.MAX_REPAIR_SLOTS}\nМеханіки: {currentWorkshopCity.mechanics}/{currentWorkshopCity.maxMechanics}";
        if (btnHireMechanic != null) { btnHireMechanic.interactable = currentWorkshopCity.mechanics < currentWorkshopCity.maxMechanics; btnHireMechanic.onClick.RemoveAllListeners(); btnHireMechanic.onClick.AddListener(() => { currentWorkshopCity.HireMechanic(); RefreshWorkshopInfo(); }); }
        if (btnUpgradeWorkshop != null) { btnUpgradeWorkshop.interactable = currentWorkshopCity.repairSlots < CityNode.MAX_REPAIR_SLOTS; btnUpgradeWorkshop.onClick.RemoveAllListeners(); btnUpgradeWorkshop.onClick.AddListener(() => { currentWorkshopCity.UpgradeWorkshop(); RefreshWorkshopInfo(); UpdateRows(); }); }
    }

    private void Update() { if (panelObj != null && panelObj.activeSelf) UpdateRows(); }

    private void UpdateRows()
    {
        if (currentWorkshopCity == null) return;

        var allVehicles = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
        List<VehicleController> vInWorkshop = new List<VehicleController>();
        foreach (var v in allVehicles) if (v.currentCity == currentWorkshopCity && v.status == VehicleStatus.Repairing) vInWorkshop.Add(v);

        int maxSlots = CityNode.MAX_REPAIR_SLOTS;
        while (activeRows.Count < maxSlots) activeRows.Add(CreateMinimalisticRow());
        while (activeRows.Count > maxSlots) { Destroy(activeRows[activeRows.Count - 1]); activeRows.RemoveAt(activeRows.Count - 1); }

        for (int i = 0; i < maxSlots; i++)
        {
            GameObject row = activeRows[i];
            bool isUnlocked = i < currentWorkshopCity.repairSlots;
            var tmp = row.GetComponentInChildren<TextMeshProUGUI>();

            if (i < vInWorkshop.Count && isUnlocked)
            {
                VehicleController v = vInWorkshop[i];
                tmp.text = $"[Слот {i + 1}] {v.vehicleData.vehicleName} - Ремонт: <color=yellow>{v.condition:F1}%</color>";
            }
            else if (!isUnlocked)
            {
                tmp.text = $"<color=#666>[Слот {i + 1}] ЗАКРИТИЙ (Апгрейд)</color>";
            }
            else
            {
                tmp.text = $"<color=#AAA>[Слот {i + 1}] ПОРОЖНІЙ</color>";
            }
        }
    }

    
    private GameObject CreateMinimalisticRow()
    {
        var row = new GameObject("RepairSlot", typeof(RectTransform));
        row.transform.SetParent(contentTransform, false);
        var rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 30); 

        var bg = row.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        var tmpObj = new GameObject("Text", typeof(RectTransform));
        tmpObj.transform.SetParent(row.transform, false);
        var tRT = tmpObj.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(10, 0); tRT.offsetMax = Vector2.zero;

        var tmp = tmpObj.AddComponent<TextMeshProUGUI>();
        tmp.color = Color.white;
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;

        return row;
    }
}