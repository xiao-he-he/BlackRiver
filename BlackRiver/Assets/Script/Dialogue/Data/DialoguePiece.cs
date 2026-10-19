using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


[System.Serializable]
public class DialoguePiece  
{
    [Header("Dialogue Piece")]
    public Sprite faceImage;

    public bool onLeft;

    public string name;

    [TextArea] 
    public string dialogueText;

    public bool hasToPause;

    [HideInInspector]
    public bool isDone;


    public UnityEvent afterTalkEvent;

}
