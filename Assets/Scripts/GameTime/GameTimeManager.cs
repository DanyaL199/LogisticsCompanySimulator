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

    [Header("Швидкість (0=пауза, 1=1x, 2=2x, 4=4x)")]
    [Range(0, 4)]
    public int timeSpeed = 1;

    [Header("Поточна дата (тільки читання)")]
    [SerializeField] private GameDate currentDate;

    private float baseSecondsPerHour = 10f / 24f;
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

        timer += Time.deltaTime * timeSpeed;

        while (timer >= baseSecondsPerHour)
        {
            timer -= baseSecondsPerHour;
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
    public void SetDate(int year, int month, int day, int hour)
    {
        currentDate = new GameDate(year, month, day, hour);
    }

    public void SetSpeed(int s)
    {
        switch (s)
        {
            case 0:
                timeSpeed = 0;
                Time.timeScale = 0f; 
                break;
            case 1:
                timeSpeed = 1;
                Time.timeScale = 1f; 
                break;
            case 2:
                timeSpeed = 2;
                Time.timeScale = 2f; 
                break;
            case 3:
                timeSpeed = 4;
                Time.timeScale = 4f; 
                break;
            default:
                timeSpeed = 1;
                Time.timeScale = 1f;
                break;
        }
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