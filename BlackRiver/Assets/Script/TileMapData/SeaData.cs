using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeaData : MonoBehaviour
{
   

    [Header("Audio Settings")]
    [SerializeField]
    private SoundDetailsList_SO snowData;
    
    private AudioMananger audioMananger;


   // private AudioSource audioSource;
 

    void Start()
    {
        audioMananger=FindObjectOfType<AudioMananger>();

        //audioSource = transform.Find("AudioManager").GetComponentInChildren<AudioSource>(true);
        //if (audioSource == null)
        //{
        //    audioSource = gameObject.AddComponent<AudioSource>();
        //}
        //audioSource.playOnAwake = false;
    }



    private void OnTriggerStay2D(Collider2D collision)
    {

        if (collision.CompareTag("Player") )
        {
            

            // 情况1：雪地
            // 随机选择一个音效
            int snowclipIndex = 21;

            audioMananger.PlayMusicClip(snowData.soundDetailsList[snowclipIndex],1f);


        }

    }
}
