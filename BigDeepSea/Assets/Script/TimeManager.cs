using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
 

public class TimeManager :Singleton<TimeManager>
{
    private int gameSeconds,gameMinute,gameHour,gameDay,gameMonth,gameYear ;
    private WeatherState gameWeather =  WeatherState.Snowy;
    private int monthInWeather = 3;

    private float tikTime;
    public bool gameClockPause;
    public TimeSpan GameTime=>new TimeSpan(gameHour,gameMinute,gameSeconds);
    private float timeDifference;//灯光时间差

    protected override void Awake()
    {
        base.Awake();
        NewGameTime();
    }

    private void OnEnable()
    {
        EventHandler.BeforeSceneUnLoadEvent+=OnBeforeSceneUnloadEvent;
        EventHandler.AfterSceneLoadedEvent+=OnAfterSceneLoadedEvent;
    }

    private void OnAfterSceneLoadedEvent()
    {
        // 时间开始走动
        gameClockPause = false;

        EventHandler.CallGameDataEvent(gameHour, gameDay, gameMonth, gameYear,gameWeather);
        EventHandler.CallGameMinuteEvent(gameSeconds, gameMinute, gameHour, gameDay, gameWeather);
        //场景加载 就执行灯光切换到白天
        EventHandler.CallLightShiftChangeEvent(gameWeather, GetCurrentLightShift(), timeDifference);
    }

    private void OnBeforeSceneUnloadEvent()
    {
        // 时间暂停一会
        gameClockPause = true;
    }

    private void OnDisable()
    {
        EventHandler.BeforeSceneUnLoadEvent-=OnBeforeSceneUnloadEvent;
        EventHandler.AfterSceneLoadedEvent-=OnAfterSceneLoadedEvent;
    }


    private void Start()
    {
        EventHandler.CallGameDataEvent(gameHour, gameDay,gameMonth,gameYear,gameWeather);
        EventHandler.CallGameMinuteEvent(gameSeconds,gameMinute,gameHour,gameDay,gameWeather);

        //起始运行就切换灯光观察效果
        EventHandler.CallLightShiftChangeEvent(gameWeather,GetCurrentLightShift(), timeDifference);
    
    }
    private void NewGameTime()
    {
        gameSeconds = 0;
        gameMinute = 0;
        gameHour =7;
        gameDay = 1;
        gameMonth = 1;
        gameYear = 2026;
        gameWeather = WeatherState.Snowy;
    }
    private void Update()
    {
        if (!gameClockPause)
        {
            tikTime += Time.deltaTime;

            if (tikTime > Settings.secondThreshold)
            {
                tikTime -= Settings.secondThreshold;
            }

            UpdateGameTime();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            #region 测试按秒钟自增
            gameSeconds++;
            EventHandler.CallGameMinuteEvent(gameSeconds, gameMinute, gameHour, gameDay, gameWeather);
            #endregion


            //for (int i = 0; i < 60; i++)
            //{
            //    UpdateGameTime();
            //}
        }
        //测试天数跳跃
        if (Input.GetKeyDown(KeyCode.G))
        {
            gameDay++;
            EventHandler.CallGameDayEvent(gameDay, gameWeather);
            EventHandler.CallGameDataEvent(gameHour, gameDay, gameMonth, gameYear, gameWeather);
        }
    
    
    }

    private void OnStartNewGameEvent(int Index)
    {
        NewGameTime();
        //新游戏开始以后不用暂停了
        gameClockPause = false;
    }
    private void OnEndGameEvent()
    {
        gameClockPause = true;
    }
    private void OnUpdateGameStateEvent(GameState gameState)
    {
        //是否需要暂停时钟
        gameClockPause = gameState == GameState.GamePause;
    }

    private void UpdateGameTime() 
    {
     
            gameSeconds++;
            if (gameSeconds >= 60)
            {
                gameMinute++;
                gameSeconds = 0;
                if (gameMinute >= 60)
                {
                    gameHour++;
                    gameMinute = 0;
                    if (gameHour >= 24)
                    {
                        gameDay++;
                        gameHour = 0;
                        if (gameDay > DateTime.DaysInMonth(gameYear, gameMonth))//30,31
                        {
                            gameDay = 1;
                            gameMonth++;
                           if (gameMonth > 12)
                               gameMonth = 1;
                            monthInWeather--;
                           if (monthInWeather == 0)
                           {
                                monthInWeather = 3;

                                int weatherNumber = (int)gameWeather;
                                weatherNumber++;
                                if (weatherNumber > 3)
                                {
                                    weatherNumber = 0;
                                    gameYear++;
                                }
                                gameWeather = (WeatherState)weatherNumber;
                                if (gameYear > 9999)
                                {
                                   gameYear = 2026;
                                }
                           }
                           //刷新地图的
                          EventHandler.CallGameDayEvent(gameDay, gameWeather); 
                            
                        }

                    }

                    EventHandler.CallGameDataEvent(gameHour,  gameDay, gameMonth,gameYear, gameWeather);
                }

              EventHandler.CallGameMinuteEvent(gameSeconds,gameMinute, gameHour, gameDay, gameWeather);
            ////切换灯光按分来计数/
            EventHandler.CallLightShiftChangeEvent(gameWeather,GetCurrentLightShift(),timeDifference);    
           }

        Debug.Log("Second:" + gameSeconds + " Minute: " + gameMinute);

    }


    private LightShift GetCurrentLightShift()
    {
        if(GameTime>=Settings.morningTime&& GameTime <= Settings.nightTime)
        {
            timeDifference = (float)(GameTime - Settings.morningTime).TotalMinutes;
            return LightShift.Morning;
        }
        if(GameTime>=Settings.nightTime&& GameTime <= Settings.morningTime)
        {
            timeDifference=Mathf.Abs((float)(GameTime-Settings.nightTime).TotalMinutes);
            //Debug.Log(timeDifference);
            return LightShift.Night;
        }

        return LightShift.Morning;
    }

    //public GameSaveData GenerateSaveData()
    //{
    //    GameSaveData saveData = new GameSaveData();

    //    saveData.timeDict = new Dictionary<string, int>();
    //    saveData.timeDict.Add("gameYear", gameYear);
    //    saveData.timeDict.Add("gameSeason", (int)gameSeason);
    //    saveData.timeDict.Add("gameMonth", gameMonth);
    //    saveData.timeDict.Add("gameDay", gameDay);
    //    saveData.timeDict.Add("gameHour", gameHour);
    //    saveData.timeDict.Add("gameMinute", gameMinute);
    //    saveData.timeDict.Add("gameSecond", gameSecond);

    //    return saveData;
    //}

    //public void RestoreData(GameSaveData saveData)
    //{
    //    gameYear = saveData.timeDict["gameYear"];
    //    gameSeason = (Season)saveData.timeDict["gameSeason"];
    //    gameMonth = saveData.timeDict["gameMonth"];
    //    gameDay = saveData.timeDict["gameDay"];
    //    gameHour = saveData.timeDict["gameHour"];
    //    gameMinute = saveData.timeDict["gameMinute"];
    //    gameSecond = saveData.timeDict["gameSecond"];
    //}
}
