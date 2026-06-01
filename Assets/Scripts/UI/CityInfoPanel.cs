using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CityInfoPanel : MonoBehaviour
{
    public static CityInfoPanel Instance { get; private set; }

    public GameObject panelObj;
    public TextMeshProUGUI cityNameText;
    public TextMeshProUGUI demandsListText;

    public Button btnBuildWorkshop;
    public Button btnHireMechanic;
    public Button btnCreateRoute;
    public Button btnClose;

    public CityNode SelectedCity { get; private set; }

    private void Awake()
    {
        Instance = this;
        if (panelObj != null) panelObj.SetActive(false);
        if (btnClose != null) btnClose.onClick.AddListener(ClosePanel);
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
    }

    private void RefreshUI()
    {
        if (SelectedCity == null) return;

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

        btnHireMechanic.interactable = SelectedCity.hasWorkshop;
        btnHireMechanic.onClick.RemoveAllListeners();
        btnHireMechanic.onClick.AddListener(() => { SelectedCity.HireMechanic(); RefreshUI(); });

        btnCreateRoute.onClick.RemoveAllListeners();
        btnCreateRoute.onClick.AddListener(() => {
            ClosePanel();
            if (RouteBuilderPanel.Instance != null) RouteBuilderPanel.Instance.OpenPanel();
        });

        // ВИВІД НОВОГО ПОПИТУ З РОЗДІЛЕННЯМ (Вантаж / Пасажири)
        string ds = "Попит:\n";
        foreach (var d in SelectedCity.demands)
        {
            ds += $"<b>До: {d.destination.cityName}</b>\n";
            ds += $"  Вантажі: {d.currentCargo}/{d.maxCargo}\n";
            ds += $"  Пасажири: {d.currentPassengers}/{d.maxPassengers}\n";
        }

        if (demandsListText != null) demandsListText.text = ds;
    }
}