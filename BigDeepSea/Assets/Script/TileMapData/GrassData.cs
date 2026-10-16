using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GrassData : MonoBehaviour
{


    [Header("Audio Settings")]
   
    [SerializeField]
    private SoundDetailsList_SO grassData;

    private AudioSource audioSource;
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

    private AudioMananger audioMananger;
    private Player player;
    public bool IsMoving
    {
        get => player.isMoving;

    }
    
    private void OnTriggerStay2D(Collider2D collision)
    {

        // 检查草地层是否有瓦片

        if (collision.CompareTag("Player"))
        {
            

            // 情况3：草地
            // 随机选择一个音效
            int grassclipIndex = Random.Range(0, 4);
           

             if(IsMoving&& (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.W)) ) 
              audioMananger.PlayMusicClip(grassData.soundDetailsList[grassclipIndex], 1.2f);//1-12 (UnityEngine.AudioClip)","1-10 (UnityEngine.AudioClip)"


            // EventHandler.CallPlaySoundEvent(SoundName.footStepGrass);
        }

    }
}
