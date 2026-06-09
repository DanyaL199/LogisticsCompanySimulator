using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class CityInfoPanel : MonoBehaviour
{
    public static CityInfoPanel Instance { get; private set; }

    public GameObject panelObj;
    public TextMeshProUGUI cityNameText;


    public Button btnBuildWorkshop;
    public Button btnCreateRoute;
    public Button btnBuildRoad;
    public Button btnClose;

    public CityNode SelectedCity { get; private set; }

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
        if (panelObj != null && panelObj.activeSelf && SelectedCity != null)
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= 0.2f)
            {
                updateTimer = 0f;
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
        UpdateDemandWorldLabels();
    }

    public void ClosePanel()
    {
        if (panelObj != null) panelObj.SetActive(false);
        SelectedCity = null;
        ClearDemandWorldLabels();
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
                    CityNode cityToPass = SelectedCity;
                    ClosePanel(); 
                    if (WorkshopPanel.Instance != null) WorkshopPanel.Instance.OpenPanel(cityToPass);
                });
            }
            else
            {
                if (workshopBtnText != null) workshopBtnText.text = "Майстерню(25тис)";
                btnBuildWorkshop.interactable = true;
                btnBuildWorkshop.onClick.RemoveAllListeners();
                btnBuildWorkshop.onClick.AddListener(() =>
                {
                    SelectedCity.BuildWorkshop();
                    RefreshUI();
                });
            }
        }


        if (btnCreateRoute != null)
        {
            btnCreateRoute.onClick.RemoveAllListeners();
            btnCreateRoute.onClick.AddListener(() => {
                CityNode cityToPass = SelectedCity;
                
                if (RouteBuilderPanel.Instance != null) RouteBuilderPanel.Instance.OpenPanel(cityToPass);
            });
        }

        if (btnBuildRoad != null)
        {
            btnBuildRoad.onClick.RemoveAllListeners();
            btnBuildRoad.onClick.AddListener(() => {
                CityNode cityToPass = SelectedCity;
                
                if (RoadBuilderPanel.Instance != null) RoadBuilderPanel.Instance.OpenPanel(cityToPass);
            });
        }

    }



    private void UpdateDemandWorldLabels()
    {
        if (SelectedCity == null || SelectedCity.demands == null) return;

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
                if (i >= demandWorldLabels.Count) demandWorldLabels.Add(CreateDemandLabel());

                GameObject labelObj = demandWorldLabels[i];
                labelObj.SetActive(true);
                labelObj.transform.position = d.destination.transform.position + new Vector3(0, 1f, 0);

                var tmp = labelObj.GetComponent<TextMeshPro>();
                if (tmp != null)
                {
                    tmp.text = $"<color=#FFAA00>В: {Mathf.FloorToInt(d.currentCargo)}</color> | <color=#00AAFF>П: {Mathf.FloorToInt(d.currentPassengers)}</color>";
                }
                i++;
            }
        }

        for (; i < demandWorldLabels.Count; i++) demandWorldLabels[i].SetActive(false);
    }

    private void ClearDemandWorldLabels()
    {
        foreach (var lbl in demandWorldLabels) if (lbl != null) Destroy(lbl);
        demandWorldLabels.Clear();
    }

    private GameObject CreateDemandLabel()
    {
        GameObject obj = new GameObject("DemandLabel");
        var tmp = obj.AddComponent<TextMeshPro>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 2.5f;
        tmp.color = Color.white;
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = Color.black;
        tmp.sortingOrder = 50;
        return obj;
    }
}