using UnityEngine;
using System;

[System.Serializable]
public struct GameDate
{
    public int year;
    public int month;
    public int day;
    public int hour;

    public GameDate(int y, int m, int d, int h)
    {
        year = y;
        month = m;
        day = d;
        hour = h;
    }

    public override string ToString()
    {
        return "Y" + year.ToString("D2") + "-M" + month.ToString("D2")
             + "-D" + day.ToString("D2") + "  " + hour.ToString("D2") + ":00";
    }

    public string ToShortString()
    {
        return "Y" + year.ToString("D2") + " M" + month.ToString("D2")
             + " D" + day.ToString("D2");
    }

    public GameDate AddHours(int hours)
    {
        GameDate result = this;
        result.hour += hours;

        while (result.hour >= 24)
        {
            result.hour -= 24;
            result.day++;

            if (result.day > 30)
            {
                result.day = 1;
                result.month++;

                if (result.month > 12)
                {
                    result.month = 1;
                    result.year++;
                }
            }
        }
        return result;
    }

    public bool IsAfter(GameDate other)
    {
        if (year != other.year) return year > other.year;
        if (month != other.month) return month > other.month;
        if (day != other.day) return day > other.day;
        return hour > other.hour;
    }

    public int HoursUntil(GameDate future)
    {
        int cur = year * 12 * 30 * 24 + month * 30 * 24 + day * 24 + hour;
        int tgt = future.year * 12 * 30 * 24 + future.month * 30 * 24
                + future.day * 24 + future.hour;
        return Mathf.Max(0, tgt - cur);
    }
}

public class GameTimeManager : MonoBehaviour
{
    public static GameTimeManager Instance { get; private set; }

    [Header("Початкова дата")]
    public int startYear = 1;
    public int startMonth = 1;
    public int startDay = 1;

    [Header("Швидкість (0=пауза, 1=1x, 2=2x, 3=4x)")]
    [Range(0, 3)]
    public int timeSpeed = 1;

    [Header("Поточна дата (тільки читання)")]
    [SerializeField] private GameDate currentDate;

    // 1 ігровий день = 10 реальних секунд при 1x
    // тому 1 година = 10/24 сек
    private float secondsPerHour_1x = 10f / 24f;
    private float secondsPerHour_2x = 5f / 24f;
    private float secondsPerHour_4x = 2.5f / 24f;

    private float timer = 0f;

    public event Action<GameDate> OnHourChanged;
    public event Action<GameDate> OnDayChanged;
    public event Action<GameDate> OnMonthChanged;
    public event Action<GameDate> OnYearChanged;

    public GameDate CurrentDate
    {
        get { return currentDate; }
    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        currentDate = new GameDate(startYear, startMonth, startDay, 0);
    }

    private void Update()
    {
        if (timeSpeed == 0) return;

        float rate;
        if (timeSpeed == 1) rate = secondsPerHour_1x;
        else if (timeSpeed == 2) rate = secondsPerHour_2x;
        else rate = secondsPerHour_4x;

        timer += Time.deltaTime;

        while (timer >= rate)
        {
            timer -= rate;
            AdvanceHour();
        }
    }

    private void AdvanceHour()
    {
        int prevDay = currentDate.day;
        int prevMonth = currentDate.month;
        int prevYear = currentDate.year;

        currentDate = currentDate.AddHours(1);

        if (OnHourChanged != null)
            OnHourChanged(currentDate);

        if (currentDate.day != prevDay)
        {
            if (OnDayChanged != null)
                OnDayChanged(currentDate);
        }

        if (currentDate.month != prevMonth)
        {
            if (OnMonthChanged != null)
                OnMonthChanged(currentDate);
        }

        if (currentDate.year != prevYear)
        {
            if (OnYearChanged != null)
                OnYearChanged(currentDate);
        }
    }

    public void SetSpeed(int s)
    {
        if (s < 0) s = 0;
        if (s > 3) s = 3;
        timeSpeed = s;
        if (timeSpeed == 0)
            timer = 0f;
    }

    public GameDate GetFutureDate(int hoursFromNow)
    {
        return currentDate.AddHours(hoursFromNow);
    }

    public bool HasPassed(GameDate date)
    {
        return currentDate.IsAfter(date);
    }
}