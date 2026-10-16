using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(AudioSource))]
public class Sound : MonoBehaviour
{
   [SerializeField]
   private AudioSource audioSource;

    /// <summary>
    /// 声音修正、声音随机播放修改pitch值在0.1到1.5之间切换
    /// </summary>
    /// <param name="soundDetails"></param>
    public void SetSound(SoundDetails soundDetails)
    {
        audioSource.clip=soundDetails.soundClip;
        audioSource.volume = soundDetails.soundVolume;
        // 0.1到1.5之间音量大小在随机变化
        audioSource.pitch=Random.Range(soundDetails.soundPitchMin,soundDetails.soundPitchMax);
    }


}
