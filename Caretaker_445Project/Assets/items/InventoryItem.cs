using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public interface IInventoryItem
{
    string Name { get; }
    Sprite Image { get; }
    void OnPickup();
    void OnDrop();
}


public class InventoryEventArgs : EventArgs
{
    public InventoryEventArgs(IInventoryItem item)
    {
        item = item;
    }
    public IInventoryItem item;
}

public class InventoryItem : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
