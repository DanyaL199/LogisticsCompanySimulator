using UnityEngine;

public class MechanicManager : MonoBehaviour
{
    public static MechanicManager Instance { get; private set; }

    [Header("Глобальний поріг відправки на ТО (%)")]
    public float globalRepairThreshold = 30f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
}