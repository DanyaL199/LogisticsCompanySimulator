using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CityInfoPanel : MonoBehaviour
{
    public static CityInfoPanel Instance { get; private set; }

    public GameObject panelObj;
    public TextMeshProUGUI cityNameText;
    public TextMeshProUGUI demandsListText;
    public Button btnBuildGarage;
    public Button btnHireMechanic;
    public Button btnCreateRoute;

    private CityNode selectedCity;

    private void Awake() { Instance = this; panelObj.SetActive(false); }

    public void OpenPanel(CityNode city)
    {
        selectedCity = city;
        cityNameText.text = city.cityName;
        panelObj.SetActive(true);
        RefreshUI();
    }

    public void ClosePanel() { panelObj.SetActive(false); }

    private void RefreshUI()
    {
        btnBuildGarage.interactable = !selectedCity.hasGarage;
        btnHireMechanic.interactable = selectedCity.hasGarage;

        btnBuildGarage.onClick.RemoveAllListeners();
        btnBuildGarage.onClick.AddListener(() => { selectedCity.BuildGarage(); RefreshUI(); });

        btnHireMechanic.onClick.RemoveAllListeners();
        btnHireMechanic.onClick.AddListener(() => { selectedCity.HireMechanic(); RefreshUI(); });

        btnCreateRoute.onClick.RemoveAllListeners();
        btnCreateRoute.onClick.AddListener(() => {
            ClosePanel();
            RouteBuilderPanel.Instance.OpenPanel();
        });

        // Виведення списку попиту
        string ds = "";
        foreach (var d in selectedCity.demands)
            ds += $"В {d.destination.cityName}: {d.currentUnits}/{d.maxUnits}\n";
        demandsListText.text = ds;
    }
}