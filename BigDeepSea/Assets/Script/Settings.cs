using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Settings  
{ 
    public const float itemFadeDuration = 0.35f;
    public const float targetAlpha = 0.45f;
    public const float fadeDuration = 1.5f;

    public const float lightChangeDuration = 30f;//change light duration
    public static TimeSpan morningTime = new TimeSpan(6, 0, 0);//day start time
    public static TimeSpan nightTime = new TimeSpan(19, 0, 0);//night start time
    public  const float secondThreshold=0.1f;
}
