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

    [Header("Кнопка закриття")]
    public Button btnClose;

    // Публічна властивість (з приватним сетом), щоб Магазин міг перевірити відкрите місто
    public CityNode SelectedCity { get; private set; }

    private void Awake()
    {
        Instance = this;
        panelObj.SetActive(false);

        // Підписуємо кнопку закриття
        if (btnClose != null)
        {
            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(ClosePanel);
        }
    }

    public void OpenPanel(CityNode city)
    {
        SelectedCity = city;
        cityNameText.text = city.cityName;
        panelObj.SetActive(true);
        RefreshUI();
    }

    public void ClosePanel()
    {
        panelObj.SetActive(false);
        SelectedCity = null; // Очищаємо вибране місто при закритті
    }

    private void RefreshUI()
    {
        if (SelectedCity == null) return;

        btnBuildGarage.interactable = !SelectedCity.hasGarage;
        btnHireMechanic.interactable = SelectedCity.hasGarage;

        btnBuildGarage.onClick.RemoveAllListeners();
        btnBuildGarage.onClick.AddListener(() => { SelectedCity.BuildGarage(); RefreshUI(); });

        btnHireMechanic.onClick.RemoveAllListeners();
        btnHireMechanic.onClick.AddListener(() => { SelectedCity.HireMechanic(); RefreshUI(); });

        btnCreateRoute.onClick.RemoveAllListeners();
        btnCreateRoute.onClick.AddListener(() => {
            ClosePanel();
            RouteBuilderPanel.Instance.OpenPanel();
        });

        // Виведення списку попиту
        string ds = "";
        foreach (var d in SelectedCity.demands)
            ds += $"В {d.destination.cityName}: {d.currentUnits}/{d.maxUnits}\n";
        demandsListText.text = ds;
    }
}