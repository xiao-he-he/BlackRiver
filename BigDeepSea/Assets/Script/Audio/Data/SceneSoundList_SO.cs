using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SceneSoundList_SO", menuName = "Sound/SceneSoundList")]

public class SceneSoundList_SO : ScriptableObject
{
    public List<SceneSoundItem> sceneSoundList;
    public SceneSoundItem GetSceneSoundItem(string  name)
    {
        return sceneSoundList.Find(s=>s.sceneName==name);
    }
}
[Serializable]
public class SceneSoundItem
{
    [SceneName] public string sceneName;
    public SoundName music;
    public SoundName ambient;
}
