using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LightManager : MonoBehaviour
{
    private LightControl[] sceneLights;

    private  LightShift currentLightShift;

    private WeatherState currentWeather;
    //初始化防止为0，防止开始新游戏时灯光逐渐变化
    private float timeDifference = 21f;// Settings.lightChangeDuration;


    private void OnEnable()
    {
        EventHandler.AfterSceneLoadedEvent += OnAfterSceneLoadedEvent;

        EventHandler.LightShiftChangeEvent += OnLightShiftChangeEvent;

         EventHandler.StartNewGameEvent += OnStartNewGameEvent;
    }

   

    private void OnDisable()
    {
        EventHandler.AfterSceneLoadedEvent -= OnAfterSceneLoadedEvent;

        EventHandler.LightShiftChangeEvent -= OnLightShiftChangeEvent;

         EventHandler.StartNewGameEvent -= OnStartNewGameEvent;
    }

    private void OnStartNewGameEvent(int obj)
    {
        //开始新游戏初始化切换为灯光的早上状态
        currentLightShift = LightShift.Morning;
    }


    private void OnAfterSceneLoadedEvent()
    {
        sceneLights = FindObjectsOfType<LightControl>();//LightControl[9]

        foreach (LightControl light in sceneLights)//LightControl[9]
        {
            //灯光控制器 改变灯光的方法
            light.ChangeLightShift(currentWeather, currentLightShift, timeDifference);
        }
    }

    private void OnLightShiftChangeEvent(WeatherState gameWeather, LightShift lightShift, float timeDifference)
    {
        currentWeather = gameWeather;
        this.timeDifference = timeDifference;
        if (currentLightShift != lightShift)
        {
            currentLightShift = lightShift;
            foreach (LightControl light in sceneLights)
            {
                //灯光控制器 改变灯光的方法
                light.ChangeLightShift(currentWeather, currentLightShift, timeDifference);
            }
        }
    }
}
