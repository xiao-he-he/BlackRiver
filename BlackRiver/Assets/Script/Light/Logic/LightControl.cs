using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class LightControl : MonoBehaviour
{
    [SerializeField]
    private LightPattenListSO lightData;

    private Light2D currentLight;
    private LightDetails currentLightDetails;


    private void Awake()
    {
        currentLight  = GetComponent<Light2D>();
    }

    //切换光照
    public void ChangeLightShift(WeatherState gameWeather, LightShift lightShift, float timeDifference) //半夜01:49   timeDifference=971
    {
        //从light patten list_SO读取你初始设置的weather，lightShift，lightColor，LightAmount//Sunny morning
        currentLightDetails = lightData.GetLightDetails(gameWeather, lightShift);//Wall Light (LightPattenList_SO)//LightDetails lightColor RGBA(0.945, 0.910, 0.557, 1.000)

        //设置灯光变化的时间周期为25秒，在30秒内缓慢变化，大于30秒直接使用初始化的设置值
        if (timeDifference < Settings.lightChangeDuration)
        {
            var colorOffset = (currentLightDetails.lightColor - currentLight.color) / Settings.lightChangeDuration * timeDifference;

            //随着时间参数变化的Color
            currentLight.color += colorOffset;
            //切换颜色 TweenerCore<float, float, FloatOptions> To(DOGetter<Color> getter, DOSetter<Color> setter, Color endValue, float duration)//Color为浮点值
            //public delegate Color DOGetter<out Color>();  ()=> myProperty
            // public delegate void DOSetter<in Color>(Color pNewValue); x=> myProperty = x
            DOTween.To(() => currentLight.color, c => currentLight.color = c, currentLightDetails.lightColor, Settings.lightChangeDuration - timeDifference);
            DOTween.To(() => currentLight.intensity, i => currentLight.intensity = i, currentLightDetails.lightAmount, Settings.lightChangeDuration - timeDifference);
        }
        if (timeDifference >= Settings.lightChangeDuration)//场景切换的时候 timeDifference=971
        {
            // public Color color { get => m_Color; set => m_Color = value; } sealed partial class Light2D
            currentLight.color = currentLightDetails.lightColor;//RGBA(0.855, 0.778, 0.551, 1.000)"-》"RGBA(0.498, 0.540, 0.811, 1.000)"
            //  public float intensity { get => m_Intensity; set => m_Intensity = value; } sealed partial class Light2D
            currentLight.intensity = currentLightDetails.lightAmount;//2、、1.8--》0.84
        }
    }
}
