using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitioner : MonoBehaviour
{// drop into any object and set the index of the scene to change to in the button
    public void ChangeToScene(int index)
    {
        SceneManager.LoadScene(index);
    }
}
