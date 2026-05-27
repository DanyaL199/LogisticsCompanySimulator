using UnityEngine;
using TMPro; // Для тексту
using UnityEngine.UI; // Для кнопок

public class CityInfoPanel : MonoBehaviour
{
    public TextMeshProUGUI cityNameText;
    public Button buildGarageBtn;
    public Button createRouteBtn;

    private CityNode selectedCity;

    // Викликається з MapClickHandler.cs при кліку на місто
    public void OpenPanel(CityNode city)
    {
        selectedCity = city;
        cityNameText.text = city.cityName;

        // Якщо гараж вже є, робимо кнопку неактивною
        buildGarageBtn.interactable = !city.hasGarage;

        gameObject.SetActive(true);
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    // Прив'яжи цю функцію до кнопки "Побудувати гараж" в Inspector (On Click)
    public void OnBuildGarageClicked()
    {
        if (selectedCity != null)
        {
            // Тут ти можеш додати перевірку грошей: if(FinanceManager.Instance.balance >= 5000)
            selectedCity.BuildGarage();
            buildGarageBtn.interactable = false; // Вимикаємо кнопку після покупки
        }
    }

    // Прив'яжи цю функцію до кнопки "Створити маршрут"
    public void OnCreateRouteClicked()
    {
        Debug.Log($"Починаємо створення маршруту з міста {selectedCity.cityName}");
        // Тут будемо викликати RouteBuilderPanel (UI створення маршрутів)
    }
}