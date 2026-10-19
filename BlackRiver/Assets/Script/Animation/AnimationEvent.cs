using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AnimationEvent : MonoBehaviour
{
    private AudioMananger m_AudioMananger;

    private void Start()
    {
        m_AudioMananger=FindObjectOfType<AudioMananger>();
        m_AudioMananger.transform.GetChild(0).GetComponent<AudioSource>().loop = false;
        m_AudioMananger.transform.GetChild(0).GetComponent<AudioSource>().playOnAwake = false;
    }
    public void FootStepSound()
    {
      
       // EventHandler.CallPlaySoundEvent(SoundName.footStepSnow);
    }

    public void MagicStaffSound()
    {
        EventHandler.CallPlaySoundEvent(SoundName.magicStaff);
    }

}
