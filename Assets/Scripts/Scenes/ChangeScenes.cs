using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScenes : MonoBehaviour
{
    public void gameEntry(string name)
    {
        SceneManager.LoadScene(name);
    }
}