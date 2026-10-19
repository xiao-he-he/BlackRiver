using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoneData : MonoBehaviour
{
    [Header("Audio Settings")]
    
    [SerializeField]
    public SoundDetailsList_SO stoneData;


    private Player player;
    public bool IsMoving
    {
        get => player.isMoving;

    }


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
    
    private void OnTriggerStay2D(Collider2D collision)
    {

        // 检查石头层是否有瓦片
        if (collision.CompareTag("Player"))
        {
            // 情况2：石头地
            // 随机选择一个音效
            int stoneclipIndex = Random.Range(4, 8);//7
           
           if(IsMoving &&(Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.W)) )
            audioMananger.PlayMusicClip(stoneData.soundDetailsList[stoneclipIndex], 1.2f);//"footstep stone 1-05 (UnityEngine.AudioClip)"

        }

            //EventHandler.CallPlaySoundEvent(SoundName.footStepStone);
    }
}
