
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
   // public Item itemPrefab;
    private Transform itemParent;

    private void OnEnable()
    {
        EventHandler.AfterSceneLoadedEvent += OnAfterSceneLoadedEvent;

    }

    private void OnDisable()
    {
        EventHandler.AfterSceneLoadedEvent -= OnAfterSceneLoadedEvent;
    }
    

    private void OnAfterSceneLoadedEvent()
    {
        itemParent = GameObject.FindWithTag("ItemParent").transform;
        if (itemParent == null)
        {
            Debug.LogError("ItemParent not found in the scene.");
            return;
        }
       
    }
  

    private void OnInstantiateItemInScene(int ID,Vector3 pos)
    {
       // var itemData = Instantiate(itemPrefab,pos,Quaternion.identity,itemParent);

       // Item.itemID = ID;
    }

    
}
