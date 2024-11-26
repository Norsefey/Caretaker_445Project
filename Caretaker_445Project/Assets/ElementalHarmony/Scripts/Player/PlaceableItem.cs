using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlaceableItem
{
    public string itemName;
    public GameObject prefab;
    public int cost;
    public Sprite icon;
    public float minPlacementDistance = 1f; // Minimum distance from other objects
    public bool requiresFlatSurface = true;  // Whether the object needs a flat surface
    public float maxSlopeAngle = 15f;       // Maximum ground slope angle in degrees
}
