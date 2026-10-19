using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider2D))]
public class DialogueController : MonoBehaviour
{
    [SerializeField]
    private UnityEvent OnFinishEvent;


    [SerializeField]
     private List<DialoguePiece> DialogueList= new List<DialoguePiece>();


    public Stack<DialoguePiece> DialogueStack;

    private void Awake()
    {
        FillDialogueStack();
    }

    private void FillDialogueStack()
    {
        DialogueStack=new Stack<DialoguePiece>();
        for (int i = DialogueList.Count-1; i > -1; i--)
        {
            DialogueList[i].isDone=false;
            DialogueStack.Push(DialogueList[i]);
        }
    }
    private bool canTalk;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
             canTalk=true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            canTalk = false;
        }
    }

    public bool isTalking;
 
    private void Update()
    {
        if (canTalk & Input.GetKeyDown(KeyCode.E)&& !isTalking)
        {
            
                StartCoroutine(DialogueRoutine());
            
            
        }
    }
    public UnityEvent onFinishEvent;//对话触发事件
    private IEnumerator DialogueRoutine()
    {

        isTalking=true;
        if(DialogueStack. TryPop(out DialoguePiece result))
        {
            EventHandler.CallShowDialogueEvent(result);
            //暂停输入
            EventHandler.CallUpdateGameStateEvent(GameState.GamePause);//Player--> OnUpdateGameStateEvent
            yield return new WaitUntil(() =>result.isDone);
            isTalking = false;
        }
        else//不能找到对话内容时，对话说完了的情况
        {
            //可以输入
            EventHandler.CallUpdateGameStateEvent(GameState.GamePlay);
            EventHandler.CallShowDialogueEvent(null);
            //填充一个null
            FillDialogueStack();
            //不在说话
            isTalking = false;

            //不能再次启动对话 不在重复打开商店UI
            if (onFinishEvent != null)//true
            {
                //调用结束事件 ShopData：Girl02_Shop (InventoryBag_SO) Count=4
                onFinishEvent?.Invoke();//方法名称 OpenShop   目标程序集类型 MyFarm.NPC.NPCFunction, Assembly-CSharp
                canTalk = false;
            }


        }

    }

}
