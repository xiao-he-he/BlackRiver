using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioMananger : Singleton<AudioMananger>
{
    [Header("音乐数据库")]
    public SoundDetailsList_SO soundDetailsData;
    public SceneSoundList_SO sceneSoundData;

    [Header("Audio Source")]
    public AudioSource ambientSource;
    public AudioSource gameSource;

    private Coroutine soundCoroutine;


    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    [Header("Snapshots")]

    public AudioMixerSnapshot normalSnapShot;
    [Tooltip("Amient Only")]
    public AudioMixerSnapshot ambientSnapShot;
    /// <summary>
    /// 静音
    /// </summary>
    [Tooltip("静音")]
    public AudioMixerSnapshot muteSnapShot;
    /// <summary>
    /// 音乐转换秒数 8秒
    /// </summary>
    private float musicTransitionSecond = 8f;

    /// <summary>
    /// 声音随机切换时机为5s-15s
    /// </summary>
    public float MusicStartSecond => Random.Range(5f, 15f);

    private void OnEnable()
    {
        EventHandler.AfterSceneLoadedEvent += OnAfterSceneLoadedEvenet;
        EventHandler.PlaySoundEvent += OnPlaySoundEvent;
        EventHandler.EndGameEvent += OnEndGameEvent;

    }



    private void OnDisable()
    {
        EventHandler.AfterSceneLoadedEvent -= OnAfterSceneLoadedEvenet;
        EventHandler.PlaySoundEvent -= OnPlaySoundEvent;
        EventHandler.EndGameEvent -= OnEndGameEvent;
    }

    protected override void Awake()
    {
        player=FindObjectOfType<Player>();  
    }

    private void OnEndGameEvent()
    {
        if (soundCoroutine != null)
            StopCoroutine(soundCoroutine);
        //静音模式
        muteSnapShot.TransitionTo(1f);
    }

    private void OnAfterSceneLoadedEvenet()
    {
        //当前场景名字
        string currentScene = SceneManager.GetActiveScene().name;

        SceneSoundItem sceneSound = sceneSoundData.GetSceneSoundItem(currentScene);
        if (sceneSound == null) return;
       //读取SO数据
        SoundDetails ambient = soundDetailsData.GetSoundDetails(sceneSound.ambient);
        SoundDetails music = soundDetailsData.GetSoundDetails(sceneSound.music);

        if (soundCoroutine != null)
            StopCoroutine(soundCoroutine);
        soundCoroutine = StartCoroutine(PlaySoundRoutine(music, ambient));

    }



    private Player player;
    public bool IsMoving
    {
        get => player.isMoving;

    }

   
    private void OnPlaySoundEvent(SoundName soundName)//magicStaff
    {
        var soundDetails=soundDetailsData.GetSoundDetails(soundName);
        if(soundDetails!=null)
        {
           if(IsMoving &&(Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.W)))
            {
              PlayMusicClip(soundDetails, 1.2f);

            }
            //EventHandler.CallInitSoundEffect(soundDetails);//"MagicStaff (UnityEngine.AudioClip)"
            else
            {
                audioMixer.SetFloat("MusicVolume", ConvertSoundVolume(0f));
                //audioMixer.SetFloat("AmbientVolume", ConvertSoundVolume(0f));
            }
        }
    }

    /// <summary>
    /// 音效延迟播放
    /// </summary>
    /// <param name="music"></param>
    /// <param name="ambient"></param>
    /// <returns></returns>
    private IEnumerator PlaySoundRoutine(SoundDetails music, SoundDetails ambient)
    {
        //音效过渡
        if (music != null && ambient != null)
        {
            PlayAmbientClip(ambient, 2f);//Fadein fadeout 设置为1秒
            yield return new WaitForSeconds(MusicStartSecond);//随机等待5-15秒
            PlayMusicClip(music, musicTransitionSecond);
        }
    }


    /// <summary>
    /// 播放背景音乐
    /// </summary>
    /// <param name="soundDetails"></param>
    public   void PlayMusicClip(SoundDetails soundDetails, float transitionTime)
    {
        audioMixer.SetFloat("MusicVolume", ConvertSoundVolume(soundDetails.soundVolume));
        gameSource.clip = soundDetails.soundClip;//"1-01 --->"1-12 
        if (gameSource.isActiveAndEnabled)
            gameSource.Play();

        normalSnapShot.TransitionTo(transitionTime);
    }


    /// <summary>
    /// 播放环境音效
    /// </summary>
    /// <param name="soundDetails"></param>
    public void PlayAmbientClip(SoundDetails soundDetails, float transitionTime)
    {
        audioMixer.SetFloat("AmbientVolume", ConvertSoundVolume(soundDetails.soundVolume));
        ambientSource.clip = soundDetails.soundClip;
        if (ambientSource.isActiveAndEnabled)
            ambientSource.Play();

        ambientSnapShot.TransitionTo(transitionTime);
    }

    private float ConvertSoundVolume(float amount)//20到负80之间
    {
        return (amount * 100 - 80);
    }

    public void SetMasterVolume(float value)
    {
        audioMixer.SetFloat("MasterVolume", (value * 100 - 80));
    }
}
