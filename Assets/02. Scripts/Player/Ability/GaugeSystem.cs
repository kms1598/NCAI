using System;
using UnityEngine;

public class GaugeSystem : MonoBehaviour
{
    public static GaugeSystem instance;
    public float max = 100f;
    public float current = 100f;

    public float regenPerSecond = 8f;
    public float regenDelay = 1f;

    public event Action<float, float> OnChanged;
    public event Action OnDepleted;

    private float regenTimer;

    void Awake()
    {
        if (instance == null) instance = this;
        if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        Notify();
    }

    void Update()
    {
        if(0f < regenTimer)
        {
            regenTimer -= Time.deltaTime;
            return;
        }

        if(current < max)
        {
            current = Mathf.Min(current + regenPerSecond * Time.deltaTime, max);
            Notify();
        }
    }

    public bool HasGauge(float cost)
    {
        return cost <= current;
    }

    public bool TryConsumeGauge(float cost)
    {
        if (current < cost) return false;
        current -= cost;
        regenTimer = regenDelay;
        Notify();
        if (current <= 0f) OnDepleted?.Invoke();
        return true;
    }

    public bool Drain(float perSecond)
    {
        if(current <= 0f)
        {
            OnDepleted?.Invoke();
            return false;
        }
        current = Mathf.Max(0f, current - perSecond * Time.deltaTime);
        regenTimer = regenDelay;
        Notify();
        return 0f < current;
    }

    void Notify()
    {
        OnChanged?.Invoke(current, max);
    }
}
