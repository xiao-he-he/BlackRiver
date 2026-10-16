using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

[CreateAssetMenu(fileName = "LightPattenListSO", menuName = "Light/Light Patten")]
public class LightPattenListSO : ScriptableObject
{
  public List<LightDetails> lightPattensList;
    public LightDetails GetLightDetails(  WeatherState weatherState, LightShift lightShift)
    {
        return lightPattensList.Find(detail => detail.lightShift == lightShift&&detail.WeatherShift== weatherState);
    }

}
[System.Serializable]
public class LightDetails
{
    public Color lightColor;
    public float lightAmount;
    public  LightShift lightShift;
    public  WeatherState WeatherShift;
}
