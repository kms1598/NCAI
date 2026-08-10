using UnityEngine;
using UnityEngine.UI;

public class GaugeUI : MonoBehaviour
{
    public Slider slider;

    void OnDisable() => GaugeSystem.instance.OnChanged -= UpdateGauge;

    void Start()
    {
        if (GaugeSystem.instance != null)
        {
            GaugeSystem.instance.OnChanged += UpdateGauge;
            UpdateGauge(GaugeSystem.instance.current, GaugeSystem.instance.max);
        }
    }

    void UpdateGauge(float current, float max)
    {
        slider.value = current / max;
    }
}