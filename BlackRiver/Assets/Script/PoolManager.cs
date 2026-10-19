using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PoolManager : MonoBehaviour
{
    public List<GameObject> poolPrebabs;
    private List<ObjectPool<GameObject>> poolEffectList = new List<ObjectPool<GameObject>>();

    //队列对象池
    private Queue<GameObject> soundQueue = new Queue<GameObject>();

    private void OnEnable()
    {
        
        EventHandler.InitSoundEffect += OnInitSoundEffect;
    }
    private void OnDisable()
    {
        
        EventHandler.InitSoundEffect -= OnInitSoundEffect;
    }
    private void Start()
    {
        CreatePool();
    }

    private void CreatePool()
    {
        foreach (GameObject item in poolPrebabs)//poolPrebabs Count = 5
        {
            Transform parent = new GameObject(item.name).transform;//"LeavesFalling01
            parent.SetParent(transform);//transform=PoolManager
            
            var newPool = new ObjectPool<GameObject>(
                () => Instantiate(item, parent),//item=LeavesFalling01 (UnityEngine.GameObject),parent=LeavesFalling01 (UnityEngine.Transform)
                e => { e.gameObject.SetActive(true); },
                e => { e.gameObject.SetActive(false); },
                e => { Destroy(e); }
                );
            poolEffectList.Add(newPool);
        }
    }

     

 

  

    #region 用队列来写 音效特效播放
    private void OnInitSoundEffect(SoundDetails soundDetails)//footstep  footStepSoft
    {
        var obj = GetPoolObject();
        obj.GetComponent<Sound>().SetSound(soundDetails);//soundDetails=footstep (UnityEngine.AudioClip//获取随机大小为1.01
        obj.SetActive(true);
        StartCoroutine(StopPlaySound(obj, soundDetails.soundClip.length));//footstep (UnityEngine.AudioClip).Length=0.1241042
    }
    private GameObject GetPoolObject()
    {
        if (soundQueue.Count < 2)
            CreateSoundPool();
        return soundQueue.Dequeue();//出列 soundQueue Count = 20
    }
    private void CreateSoundPool()
    {
        //Instantiate failed because the clone was destroyed during creation.
        //This can happen if DestroyImmediate is called in MonoBehaviour.Awake.
        var parent = new GameObject(poolPrebabs[4].name).transform;//"Sound (UnityEngine.Transform)"
        parent.SetParent(transform);//PoolManager UnityEngine.Transform "(0.00, 0.00, 0.00)"
        for (int i = 0; i < 20; i++)
        {
            GameObject newobj = Instantiate(poolPrebabs[4], parent);//Sound(Clone) (UnityEngine.GameObject)
            newobj.SetActive(false);
            soundQueue.Enqueue(newobj);//入列       
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj">声音预制件</param>
    /// <param name="duration">声音片段的播放时间长度</param>
    /// <returns></returns>
    private IEnumerator StopPlaySound(GameObject obj, float duration)
    {
        yield return new WaitForSeconds(duration);
        obj.SetActive(false);
        soundQueue.Enqueue(obj);//入列
    }
    #endregion 音效特效播放

}
