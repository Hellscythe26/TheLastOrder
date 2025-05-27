using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitGame : MonoBehaviour
{
    void Start()
    {
        
    }

    void Update()
    {
        
    }

    /// <summary>
    /// Cierra la aplicación del juego.
    /// Este método es público para ser llamado desde eventos de UI (como OnClick de un botón).
    /// </summary>
    public void exit()
    {
        Application.Quit();
    }
}