using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;

public class SnowData : MonoBehaviour
{

    [Header("Audio Settings")]
    [SerializeField]
    private SoundDetailsList_SO snowData;

  
    private AudioSource audioSource;
    private Player player;

    private void Awake()
    {
        player = FindObjectOfType<Player>();
    }
    void Start()
    {
        audioMananger = FindObjectOfType<AudioMananger>();
        
        //audioSource = transform.Find("AudioManager").GetChild(0).GetComponent<AudioSource>();
        //if (audioSource == null)
        //{
        //    audioSource = gameObject.AddComponent<AudioSource>();
        //}
        //audioSource.playOnAwake = false;
    }
    public bool IsMoving
    {
        get => player.isMoving;
        
    }

    private AudioMananger audioMananger;
    
    private void OnTriggerStay2D(Collider2D collision)
    {

        // 检查雪地层是否有瓦片
        if (collision.CompareTag("Player"))
        {
            // 情况1：雪地
            // 随机选择一个音效
            int snowclipIndex = Random.Range(8,17);//12
            
            if(IsMoving&&(Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.W)))
              audioMananger.PlayMusicClip(snowData.soundDetailsList[snowclipIndex], 1.2f);//"1-01 (UnityEngine.AudioClip)""1-05 (UnityEngine.AudioClip)"
        }

            //EventHandler.CallPlaySoundEvent(SoundName.footStepSnow);
    }
}
