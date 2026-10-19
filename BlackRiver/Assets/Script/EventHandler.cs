using HeyHeyThere.WeatherFXFree;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EventHandler 
{

    public static event Action<string,Vector3> TransitionEvent;

    public static void CallTransitionEvent(string sceneToGo, Vector3 positionToGo)
    {
        TransitionEvent?.Invoke(sceneToGo, positionToGo);
    }

    public static event Action  BeforeSceneUnLoadEvent;
    public static void CallBeforeSceneUnLoadEvent()
    {
        BeforeSceneUnLoadEvent?.Invoke();
    }

    public static event Action AfterSceneLoadedEvent;
    public static void CallAfterSceneLoadedEvent()
    {
        AfterSceneLoadedEvent?.Invoke();
    }

    public static event Action<Vector3> MoveToPositionEvent;

    public static void CallMoveToPositionEvent(Vector3 taragetPosition)
    {
        MoveToPositionEvent?.Invoke(taragetPosition);
    }

    public static event Action<DialoguePiece> ShowDialogueEvent;
    public static void CallShowDialogueEvent(DialoguePiece piece)
    {
        ShowDialogueEvent?.Invoke(piece);
    }

    //小时变天，天变月，月变年，天气变化事件
    public static event Action<int, int, int, int,  WeatherState> GameDataEvent;
    public static void CallGameDataEvent(int gameHour, int gameDay, int gameMonth, int gameYear,  WeatherState gameWeather)
    {
        GameDataEvent?.Invoke(gameHour, gameDay, gameMonth, gameYear, gameWeather);
    }

    public static event Action<int,int,int,int, WeatherState> GameMinuteEvent;
    public static void CallGameMinuteEvent(int gameSecond,int gameMinute, int gameHour, int gameDay, WeatherState gameWeather)
    {
        GameMinuteEvent?.Invoke(gameSecond,gameMinute, gameHour, gameDay, gameWeather);
    }

    public static event Action<int , WeatherState> GameDayEvent;
    public static void CallGameDayEvent(int gameDay,  WeatherState gameWeather)
    {
        GameDayEvent?.Invoke(gameDay, gameWeather);
    }


    public static event Action< WeatherState, LightShift,float> LightShiftChangeEvent;
    public static void CallLightShiftChangeEvent( WeatherState gameWeather, LightShift lightShift,float timeDifference)
    {
        LightShiftChangeEvent?.Invoke(gameWeather, lightShift,timeDifference);
    }

    /// <summary>
    /// 从保存的游戏数据中加载游戏进度 开始新游戏的事件
    /// </summary>
    public static event Action<int> StartNewGameEvent;
    public static void CallStartNewGameEvent(int index)
    {
        StartNewGameEvent?.Invoke(index);
    }
    /// <summary>
    /// 切换游戏状态的事件暂停还是运行
    /// </summary>
    public static event Action<GameState> UpdateGameStateEvent;
    /// <summary>
    /// 变更游戏状态、暂停还是运行
    /// </summary>
    /// <param name="gameState">游戏状态</param>
    public static void CallUpdateGameStateEvent(GameState gameState)
    {
        UpdateGameStateEvent?.Invoke(gameState);//UpdateGameStateEvent--->Void OnUpdateGameStateEvent(GameState)
    }


    public static event Action<SoundName> PlaySoundEvent;
    public static void CallPlaySoundEvent(SoundName soundName) 
    {
    
       PlaySoundEvent?.Invoke(soundName);
    }

    public static event Action EndGameEvent;
    public static void CallEndGameEvent( ) { 
       EndGameEvent?.Invoke();
    }

    public static event Action<SoundDetails> InitSoundEffect;

    public static void CallInitSoundEffect(SoundDetails soundDetails) {
    
      InitSoundEffect?.Invoke(soundDetails);
    }
        
}
