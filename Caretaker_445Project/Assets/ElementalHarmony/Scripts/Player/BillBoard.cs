using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillBoard : MonoBehaviour
{
    private Camera mainCamera;

    private void Start()
    {
        // Cache the main camera reference for performance
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCamera != null)
        {
            // Make the UI element face the camera
            transform.LookAt(transform.position + mainCamera.transform.forward);
        }
    }
}
